using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.Win32;

namespace FileConverter.ModernShell;

internal static class ShellData
{
    private static readonly object PresetLock = new();
    private static IReadOnlyList<Preset>? presetCache;
    private static DateTime presetCacheUntilUtc;

    internal static string? ApplicationPath
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\FileConverter");
            return key?.GetValue("Path") as string;
        }
    }

    internal static string IconPath => ApplicationPath is string path ? path + ",0" : string.Empty;

    internal static bool DisplayPresetIcons
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\FileConverter");
            return bool.TryParse(key?.GetValue("DisplayPresetIcons") as string, out bool enabled) && enabled;
        }
    }

    internal static IReadOnlyList<Preset> LoadPresets()
    {
        lock (PresetLock)
        {
            if (presetCache is not null && DateTime.UtcNow < presetCacheUntilUtc) return presetCache;
            presetCache = ReadPresets();
            presetCacheUntilUtc = DateTime.UtcNow.AddSeconds(3);
            return presetCache;
        }
    }

    private static IReadOnlyList<Preset> ReadPresets()
    {
        string? app = ApplicationPath;
        if (string.IsNullOrWhiteSpace(app)) return Array.Empty<Preset>();
        string user = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FileConverter", "Settings.user.xml");
        string defaults = Path.Combine(Path.GetDirectoryName(app)!, "Settings.default.xml");
        string source = File.Exists(user) ? user : defaults;
        if (!File.Exists(source)) return Array.Empty<Preset>();

        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            var document = new XmlDocument { XmlResolver = null };
            using var reader = XmlReader.Create(source, settings);
            document.Load(reader);
            var results = new List<Preset>();
            foreach (XmlElement element in document.SelectNodes("/Settings/ConversionPreset")!)
            {
                string name = element.GetAttribute("Name");
                var inputTypes = element.SelectNodes("InputTypes")!.Cast<XmlElement>()
                    .Select(x => x.InnerText.Trim().TrimStart('.').ToLowerInvariant()).Where(x => x.Length > 0).ToHashSet();
                if (name.Length > 0 && inputTypes.Count > 0) results.Add(new Preset(name, inputTypes));
            }
            return results;
        }
        catch
        {
            return Array.Empty<Preset>();
        }
    }

    internal static string[] SelectedPaths(IShellItemArray? items)
    {
        if (items is null || items.GetCount(out uint count) < 0 || count == 0) return Array.Empty<string>();
        var result = new List<string>((int)Math.Min(count, 4096));
        for (uint i = 0; i < count; i++)
        {
            if (items.GetItemAt(i, out IShellItem item) < 0) continue;
            try
            {
                if (item.GetDisplayName(0x80058000, out nint path) >= 0 && path != 0)
                {
                    try { result.Add(Marshal.PtrToStringUni(path)!); }
                    finally { Marshal.FreeCoTaskMem(path); }
                }
            }
            finally { Marshal.ReleaseComObject(item); }
        }
        return result.ToArray();
    }

    internal static bool SupportsAll(Preset preset, string[] paths) => paths.Length > 0 && paths.All(path =>
    {
        string extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return preset.InputTypes.Contains(extension);
    });

    internal static int Launch(string? preset, IShellItemArray? items)
    {
        string? app = ApplicationPath;
        if (string.IsNullOrWhiteSpace(app) || !File.Exists(app)) return unchecked((int)0x80070002);
        string[] paths = SelectedPaths(items);
        if (preset is not null && paths.Length == 0) return unchecked((int)0x80070057);

        var start = new ProcessStartInfo(app)
        {
            UseShellExecute = false,
            CreateNoWindow = false,
            WorkingDirectory = Path.GetDirectoryName(app)!
        };
        if (preset is null) start.ArgumentList.Add("--settings");
        else
        {
            start.ArgumentList.Add("--conversion-preset");
            start.ArgumentList.Add(preset);
            string list = Path.Combine(Path.GetTempPath(), $"file-converter-modern-{Guid.NewGuid():N}.txt");
            try
            {
                File.WriteAllLines(list, paths);
                start.ArgumentList.Add("--input-files");
                start.ArgumentList.Add(list);
                var process = Process.Start(start);
                if (process is null) { File.Delete(list); return unchecked((int)0x80004005); }
                process.EnableRaisingEvents = true;
                process.Exited += (_, _) => { try { File.Delete(list); } catch { } };
                return 0;
            }
            catch
            {
                try { File.Delete(list); } catch { }
                return unchecked((int)0x80004005);
            }
        }
        try { return Process.Start(start) is null ? unchecked((int)0x80004005) : 0; }
        catch { return unchecked((int)0x80004005); }
    }
}

internal sealed record Preset(string FullName, HashSet<string> InputTypes)
{
    internal string DisplayName => FullName.Replace('/', '\\');
}

[ComVisible(true), Guid(ClassIds.Root), ClassInterface(ClassInterfaceType.None)]
public sealed class FileConverterCommand : IExplorerCommand
{
    public int GetTitle(IShellItemArray? items, out nint title) { title = Marshal.StringToCoTaskMemUni("File Converter"); return 0; }
    public int GetIcon(IShellItemArray? items, out nint icon) { icon = Marshal.StringToCoTaskMemUni(ShellData.IconPath); return 0; }
    public int GetToolTip(IShellItemArray? items, out nint tooltip) { tooltip = 0; return unchecked((int)0x80004001); }
    public int GetCanonicalName(out Guid name) { name = Guid.Empty; return 0; }
    public int GetState(IShellItemArray? items, bool okToBeSlow, out uint state)
    {
        state = ShellData.LoadPresets().Any(p => ShellData.SupportsAll(p, ShellData.SelectedPaths(items))) ? 0u : 2u;
        return 0;
    }
    public int Invoke(IShellItemArray? items, IBindCtx? context) => ShellData.Launch(null, items);
    public int GetFlags(out uint flags) { flags = 1; return 0; }
    public int EnumSubCommands(out IEnumExplorerCommand commands)
    {
        var entries = ShellData.LoadPresets().Select(p => (IExplorerCommand)new PresetCommand(p)).ToList();
        entries.Add(new PresetCommand(null));
        commands = new ExplorerCommandEnumerator(entries);
        return 0;
    }
}

[ComVisible(true), ClassInterface(ClassInterfaceType.None)]
internal sealed class PresetCommand(Preset? preset) : IExplorerCommand
{
    public int GetTitle(IShellItemArray? items, out nint title)
    {
        string label = preset?.DisplayName ?? "Configure presets...";
        title = Marshal.StringToCoTaskMemUni(label);
        return 0;
    }
    public int GetIcon(IShellItemArray? items, out nint icon)
    {
        if (preset is not null && !ShellData.DisplayPresetIcons) { icon = 0; return unchecked((int)0x80004001); }
        icon = Marshal.StringToCoTaskMemUni(ShellData.IconPath);
        return 0;
    }
    public int GetToolTip(IShellItemArray? items, out nint tooltip) { tooltip = 0; return unchecked((int)0x80004001); }
    public int GetCanonicalName(out Guid name) { name = Guid.Empty; return 0; }
    public int GetState(IShellItemArray? items, bool okToBeSlow, out uint state)
    {
        // Unsupported conversions should disappear instead of remaining visible as disabled entries.
        state = preset is null || ShellData.SupportsAll(preset, ShellData.SelectedPaths(items)) ? 0u : 2u;
        return 0;
    }
    public int Invoke(IShellItemArray? items, IBindCtx? context) => ShellData.Launch(preset?.FullName, items);
    public int GetFlags(out uint flags) { flags = 0; return 0; }
    public int EnumSubCommands(out IEnumExplorerCommand commands) { commands = null!; return unchecked((int)0x80004001); }
}

[ComVisible(true), Guid(ClassIds.Enumerator), ClassInterface(ClassInterfaceType.None)]
internal sealed class ExplorerCommandEnumerator(IReadOnlyList<IExplorerCommand> commands) : IEnumExplorerCommand
{
    private int index;
    public int Next(uint count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0, ArraySubType = UnmanagedType.Interface)] IExplorerCommand[] values, out uint fetched)
    {
        fetched = 0;
        while (fetched < count && index < commands.Count) values[fetched++] = commands[index++];
        return fetched == count ? 0 : 1;
    }
    public int Skip(uint count) { index = Math.Min(commands.Count, index + (int)count); return index < commands.Count ? 0 : 1; }
    public int Reset() { index = 0; return 0; }
    public int Clone(out IEnumExplorerCommand clone) { clone = new ExplorerCommandEnumerator(commands) { index = index }; return 0; }
}

internal static class ClassIds
{
    internal const string Root = "F7F88B22-C4C7-4B11-A246-5D954E594D42";
    internal const string Enumerator = "A88826F8-186F-4987-AADE-EA0CEF8FBFE8";
}

[ComImport, Guid("A08CE4D0-FA25-44AB-B57C-C7B1C323E0B9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IExplorerCommand
{
    [PreserveSig] int GetTitle(IShellItemArray? items, out nint title);
    [PreserveSig] int GetIcon(IShellItemArray? items, out nint icon);
    [PreserveSig] int GetToolTip(IShellItemArray? items, out nint tooltip);
    [PreserveSig] int GetCanonicalName(out Guid name);
    [PreserveSig] int GetState(IShellItemArray? items, [MarshalAs(UnmanagedType.Bool)] bool okToBeSlow, out uint state);
    [PreserveSig] int Invoke(IShellItemArray? items, IBindCtx? context);
    [PreserveSig] int GetFlags(out uint flags);
    [PreserveSig] int EnumSubCommands(out IEnumExplorerCommand commands);
}

[ComImport, Guid("A88826F8-186F-4987-AADE-EA0CEF8FBFE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IEnumExplorerCommand
{
    [PreserveSig] int Next(uint count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0, ArraySubType = UnmanagedType.Interface)] IExplorerCommand[] values, out uint fetched);
    [PreserveSig] int Skip(uint count);
    [PreserveSig] int Reset();
    [PreserveSig] int Clone(out IEnumExplorerCommand clone);
}

[ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItemArray
{
    [PreserveSig] int BindToHandler(nint bindContext, ref Guid handler, ref Guid iid, out nint result);
    [PreserveSig] int GetPropertyStore(uint flags, ref Guid iid, out nint result);
    [PreserveSig] int GetPropertyDescriptionList(nint keyType, ref Guid iid, out nint result);
    [PreserveSig] int GetAttributes(uint flags, uint mask, out uint attributes);
    [PreserveSig] int GetCount(out uint count);
    [PreserveSig] int GetItemAt(uint index, out IShellItem item);
    [PreserveSig] int EnumItems(out nint enumerator);
}

[ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItem
{
    [PreserveSig] int BindToHandler(nint bindContext, ref Guid handler, ref Guid iid, out nint result);
    [PreserveSig] int GetParent(out IShellItem parent);
    [PreserveSig] int GetDisplayName(uint nameType, out nint name);
    [PreserveSig] int GetAttributes(uint mask, out uint attributes);
    [PreserveSig] int Compare(IShellItem other, uint hint, out int order);
}

[ComImport, Guid("0000000D-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IBindCtx { }
