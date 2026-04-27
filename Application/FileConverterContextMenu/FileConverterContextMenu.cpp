#include <windows.h>
#include <shobjidl.h>

#include <algorithm>
#include <string>
#include <vector>

namespace
{
    constexpr CLSID CLSID_FileConverterRootCommand = { 0xc069db02, 0xf64b, 0x4651, { 0xa6, 0x9f, 0x42, 0xe3, 0xb0, 0xb9, 0x4c, 0x44 } };
    constexpr ULONG ECF_HASSUBCOMMANDS_VALUE = 0x00000001;
    constexpr size_t MaximumCommandLineLength = 30000;

    HINSTANCE g_module = nullptr;
    long g_objectCount = 0;
    long g_lockCount = 0;

    PWSTR DuplicateString(const std::wstring& value)
    {
        size_t byteCount = (value.length() + 1) * sizeof(wchar_t);
        auto result = static_cast<PWSTR>(CoTaskMemAlloc(byteCount));
        if (result == nullptr)
        {
            return nullptr;
        }

        memcpy(result, value.c_str(), byteCount);
        return result;
    }

    std::wstring QuoteArgument(const std::wstring& value)
    {
        std::wstring quoted = L"\"";
        for (wchar_t character : value)
        {
            if (character == L'\\' || character == L'"')
            {
                quoted += L'\\';
            }

            quoted += character;
        }

        quoted += L"\"";
        return quoted;
    }

    std::wstring GetModuleDirectory()
    {
        wchar_t modulePath[MAX_PATH] = {};
        GetModuleFileNameW(g_module, modulePath, ARRAYSIZE(modulePath));

        std::wstring directory = modulePath;
        size_t slashIndex = directory.find_last_of(L"\\/");
        if (slashIndex != std::wstring::npos)
        {
            directory.resize(slashIndex + 1);
        }

        return directory;
    }

    std::wstring CombinePath(const std::wstring& directory, const std::wstring& fileName)
    {
        if (directory.empty())
        {
            return fileName;
        }

        wchar_t lastCharacter = directory[directory.length() - 1];
        if (lastCharacter == L'\\' || lastCharacter == L'/')
        {
            return directory + fileName;
        }

        return directory + L"\\" + fileName;
    }

    bool FileExists(const std::wstring& path)
    {
        DWORD attributes = GetFileAttributesW(path.c_str());
        return attributes != INVALID_FILE_ATTRIBUTES && (attributes & FILE_ATTRIBUTE_DIRECTORY) == 0;
    }

    std::wstring GetConfiguredInstallDirectory()
    {
        HKEY key = nullptr;
        wchar_t value[MAX_PATH] = {};
        DWORD valueSize = sizeof(value);
        DWORD valueType = REG_SZ;

        if (RegOpenKeyExW(HKEY_CURRENT_USER, L"Software\\FileConverter", 0, KEY_READ, &key) == ERROR_SUCCESS)
        {
            LSTATUS status = RegQueryValueExW(key, L"InstallLocation", nullptr, &valueType, reinterpret_cast<LPBYTE>(value), &valueSize);
            RegCloseKey(key);
            if (status == ERROR_SUCCESS && valueType == REG_SZ && value[0] != L'\0')
            {
                return value;
            }
        }

        return L"";
    }

    std::wstring GetFileConverterPath()
    {
        std::wstring configuredPath = CombinePath(GetConfiguredInstallDirectory(), L"FileConverter.exe");
        if (FileExists(configuredPath))
        {
            return configuredPath;
        }

        return CombinePath(GetModuleDirectory(), L"FileConverter.exe");
    }

    std::vector<std::wstring> GetSelectedPaths(IShellItemArray* itemArray)
    {
        std::vector<std::wstring> paths;
        if (itemArray == nullptr)
        {
            return paths;
        }

        DWORD count = 0;
        if (FAILED(itemArray->GetCount(&count)))
        {
            return paths;
        }

        for (DWORD index = 0; index < count; ++index)
        {
            IShellItem* item = nullptr;
            if (FAILED(itemArray->GetItemAt(index, &item)) || item == nullptr)
            {
                continue;
            }

            PWSTR path = nullptr;
            if (SUCCEEDED(item->GetDisplayName(SIGDN_FILESYSPATH, &path)) && path != nullptr)
            {
                paths.emplace_back(path);
                CoTaskMemFree(path);
            }

            item->Release();
        }

        return paths;
    }

    bool ShowInNewShellContextMenu()
    {
        HKEY key = nullptr;
        DWORD value = 1;
        DWORD valueSize = sizeof(value);
        DWORD valueType = REG_DWORD;

        if (RegOpenKeyExW(HKEY_CURRENT_USER, L"Software\\FileConverter", 0, KEY_READ, &key) == ERROR_SUCCESS)
        {
            RegQueryValueExW(key, L"ShowInNewShellContextMenu", nullptr, &valueType, reinterpret_cast<LPBYTE>(&value), &valueSize);
            RegCloseKey(key);
        }

        return value != 0;
    }

    HRESULT LaunchPreset(const std::wstring& presetName, IShellItemArray* itemArray)
    {
        std::vector<std::wstring> selectedPaths = GetSelectedPaths(itemArray);
        if (selectedPaths.empty())
        {
            return S_OK;
        }

        std::wstring executablePath = GetFileConverterPath();
        std::wstring commandLine = QuoteArgument(executablePath) + L" --conversion-preset " + QuoteArgument(presetName);

        for (const std::wstring& path : selectedPaths)
        {
            commandLine += L" " + QuoteArgument(path);
            if (commandLine.length() > MaximumCommandLineLength)
            {
                return E_INVALIDARG;
            }
        }

        STARTUPINFOW startupInfo = {};
        startupInfo.cb = sizeof(startupInfo);
        PROCESS_INFORMATION processInformation = {};

        std::vector<wchar_t> mutableCommandLine(commandLine.begin(), commandLine.end());
        mutableCommandLine.push_back(L'\0');

        if (!CreateProcessW(executablePath.c_str(), mutableCommandLine.data(), nullptr, nullptr, FALSE, 0, nullptr, nullptr, &startupInfo, &processInformation))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }

        CloseHandle(processInformation.hThread);
        CloseHandle(processInformation.hProcess);
        return S_OK;
    }

    class ExplorerCommand final : public IExplorerCommand
    {
    public:
        ExplorerCommand(std::wstring title, std::wstring presetName, bool hasSubCommands) :
            refCount(1),
            title(std::move(title)),
            presetName(std::move(presetName)),
            hasSubCommands(hasSubCommands)
        {
            InterlockedIncrement(&g_objectCount);
        }

        ~ExplorerCommand()
        {
            InterlockedDecrement(&g_objectCount);
        }

        IFACEMETHODIMP QueryInterface(REFIID riid, void** object) override
        {
            if (object == nullptr)
            {
                return E_POINTER;
            }

            *object = nullptr;
            if (riid == IID_IUnknown || riid == IID_IExplorerCommand)
            {
                *object = static_cast<IExplorerCommand*>(this);
                AddRef();
                return S_OK;
            }

            return E_NOINTERFACE;
        }

        IFACEMETHODIMP_(ULONG) AddRef() override
        {
            return InterlockedIncrement(&this->refCount);
        }

        IFACEMETHODIMP_(ULONG) Release() override
        {
            ULONG count = InterlockedDecrement(&this->refCount);
            if (count == 0)
            {
                delete this;
            }

            return count;
        }

        IFACEMETHODIMP GetTitle(IShellItemArray*, PWSTR* name) override
        {
            if (name == nullptr)
            {
                return E_POINTER;
            }

            *name = DuplicateString(this->title);
            return *name == nullptr ? E_OUTOFMEMORY : S_OK;
        }

        IFACEMETHODIMP GetIcon(IShellItemArray*, PWSTR* icon) override
        {
            if (icon == nullptr)
            {
                return E_POINTER;
            }

            *icon = DuplicateString(GetFileConverterPath());
            return *icon == nullptr ? E_OUTOFMEMORY : S_OK;
        }

        IFACEMETHODIMP GetToolTip(IShellItemArray*, PWSTR* tooltip) override
        {
            if (tooltip == nullptr)
            {
                return E_POINTER;
            }

            *tooltip = DuplicateString(L"Convert selected files with File Converter.");
            return *tooltip == nullptr ? E_OUTOFMEMORY : S_OK;
        }

        IFACEMETHODIMP GetCanonicalName(GUID* commandName) override
        {
            if (commandName == nullptr)
            {
                return E_POINTER;
            }

            *commandName = CLSID_FileConverterRootCommand;
            return S_OK;
        }

        IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE* state) override
        {
            if (state == nullptr)
            {
                return E_POINTER;
            }

            *state = ShowInNewShellContextMenu() ? ECS_ENABLED : ECS_HIDDEN;
            return S_OK;
        }

        IFACEMETHODIMP Invoke(IShellItemArray* itemArray, IBindCtx*) override
        {
            if (this->hasSubCommands)
            {
                return E_NOTIMPL;
            }

            return LaunchPreset(this->presetName, itemArray);
        }

        IFACEMETHODIMP GetFlags(EXPCMDFLAGS* flags) override
        {
            if (flags == nullptr)
            {
                return E_POINTER;
            }

            *flags = this->hasSubCommands ? static_cast<EXPCMDFLAGS>(ECF_HASSUBCOMMANDS_VALUE) : ECF_DEFAULT;
            return S_OK;
        }

        IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** enumCommands) override;

    private:
        long refCount;
        std::wstring title;
        std::wstring presetName;
        bool hasSubCommands;
    };

    class ExplorerCommandEnumerator final : public IEnumExplorerCommand
    {
    public:
        ExplorerCommandEnumerator() : refCount(1), position(0)
        {
            this->commands.push_back(new ExplorerCommand(L"To Jpg", L"To Jpg", false));
            this->commands.push_back(new ExplorerCommand(L"To Png", L"To Png", false));
            this->commands.push_back(new ExplorerCommand(L"To Pdf", L"To Pdf", false));
            this->commands.push_back(new ExplorerCommand(L"To Mp3", L"To Mp3", false));
            this->commands.push_back(new ExplorerCommand(L"To Mp4", L"To Mp4", false));
            InterlockedIncrement(&g_objectCount);
        }

        ~ExplorerCommandEnumerator()
        {
            for (IExplorerCommand* command : this->commands)
            {
                command->Release();
            }

            InterlockedDecrement(&g_objectCount);
        }

        IFACEMETHODIMP QueryInterface(REFIID riid, void** object) override
        {
            if (object == nullptr)
            {
                return E_POINTER;
            }

            *object = nullptr;
            if (riid == IID_IUnknown || riid == IID_IEnumExplorerCommand)
            {
                *object = static_cast<IEnumExplorerCommand*>(this);
                AddRef();
                return S_OK;
            }

            return E_NOINTERFACE;
        }

        IFACEMETHODIMP_(ULONG) AddRef() override
        {
            return InterlockedIncrement(&this->refCount);
        }

        IFACEMETHODIMP_(ULONG) Release() override
        {
            ULONG count = InterlockedDecrement(&this->refCount);
            if (count == 0)
            {
                delete this;
            }

            return count;
        }

        IFACEMETHODIMP Next(ULONG count, IExplorerCommand** command, ULONG* fetched) override
        {
            if (command == nullptr)
            {
                return E_POINTER;
            }

            ULONG actual = 0;
            while (actual < count && this->position < this->commands.size())
            {
                command[actual] = this->commands[this->position];
                command[actual]->AddRef();
                ++this->position;
                ++actual;
            }

            if (fetched != nullptr)
            {
                *fetched = actual;
            }

            return actual == count ? S_OK : S_FALSE;
        }

        IFACEMETHODIMP Skip(ULONG count) override
        {
            this->position = (std::min)(this->commands.size(), this->position + count);
            return this->position < this->commands.size() ? S_OK : S_FALSE;
        }

        IFACEMETHODIMP Reset() override
        {
            this->position = 0;
            return S_OK;
        }

        IFACEMETHODIMP Clone(IEnumExplorerCommand** enumCommands) override
        {
            if (enumCommands == nullptr)
            {
                return E_POINTER;
            }

            *enumCommands = nullptr;
            return E_NOTIMPL;
        }

    private:
        long refCount;
        std::vector<IExplorerCommand*> commands;
        size_t position;
    };

    IFACEMETHODIMP ExplorerCommand::EnumSubCommands(IEnumExplorerCommand** enumCommands)
    {
        if (enumCommands == nullptr)
        {
            return E_POINTER;
        }

        *enumCommands = nullptr;
        if (!this->hasSubCommands)
        {
            return E_NOTIMPL;
        }

        *enumCommands = new ExplorerCommandEnumerator();
        return *enumCommands == nullptr ? E_OUTOFMEMORY : S_OK;
    }

    class ClassFactory final : public IClassFactory
    {
    public:
        ClassFactory() : refCount(1)
        {
        }

        IFACEMETHODIMP QueryInterface(REFIID riid, void** object) override
        {
            if (object == nullptr)
            {
                return E_POINTER;
            }

            *object = nullptr;
            if (riid == IID_IUnknown || riid == IID_IClassFactory)
            {
                *object = static_cast<IClassFactory*>(this);
                AddRef();
                return S_OK;
            }

            return E_NOINTERFACE;
        }

        IFACEMETHODIMP_(ULONG) AddRef() override
        {
            return InterlockedIncrement(&this->refCount);
        }

        IFACEMETHODIMP_(ULONG) Release() override
        {
            ULONG count = InterlockedDecrement(&this->refCount);
            if (count == 0)
            {
                delete this;
            }

            return count;
        }

        IFACEMETHODIMP CreateInstance(IUnknown* outer, REFIID riid, void** object) override
        {
            if (outer != nullptr)
            {
                return CLASS_E_NOAGGREGATION;
            }

            auto command = new ExplorerCommand(L"File Converter", std::wstring(), true);
            if (command == nullptr)
            {
                return E_OUTOFMEMORY;
            }

            HRESULT result = command->QueryInterface(riid, object);
            command->Release();
            return result;
        }

        IFACEMETHODIMP LockServer(BOOL lock) override
        {
            if (lock)
            {
                InterlockedIncrement(&g_lockCount);
            }
            else
            {
                InterlockedDecrement(&g_lockCount);
            }

            return S_OK;
        }

    private:
        long refCount;
    };
}

STDAPI DllGetClassObject(REFCLSID classId, REFIID riid, void** object)
{
    if (classId != CLSID_FileConverterRootCommand)
    {
        return CLASS_E_CLASSNOTAVAILABLE;
    }

    auto factory = new ClassFactory();
    if (factory == nullptr)
    {
        return E_OUTOFMEMORY;
    }

    HRESULT result = factory->QueryInterface(riid, object);
    factory->Release();
    return result;
}

STDAPI DllCanUnloadNow()
{
    return g_objectCount == 0 && g_lockCount == 0 ? S_OK : S_FALSE;
}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        g_module = module;
        DisableThreadLibraryCalls(module);
    }

    return TRUE;
}
