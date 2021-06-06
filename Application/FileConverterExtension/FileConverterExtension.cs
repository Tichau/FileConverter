// <copyright file="FileConverterExtension.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverterExtension
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Forms;

    using SharpShell.Attributes;
    using SharpShell.SharpContextMenu;

    /// <summary>
    /// File converter context menu extension class.
    /// </summary>
    [ComVisible(true), Guid("AF9B72B5-F4E4-44B0-A3D9-B55B748EFE90")]
    [COMServerAssociation(AssociationType.AllFiles)]
    public class FileConverterExtension : SharpContextMenu
    {
        private PresetReference[] presetReferences = null;
        private List<MenuEntry> menuEntries = new List<MenuEntry>();

        private HashSet<string> extensionCache = new HashSet<string>();

        private class MenuEntry
        {
            public PresetReference PresetReference;
            public bool Enabled;
            public int ExtensionRefCount;

            public MenuEntry(PresetReference presetReference)
            {
                this.PresetReference = presetReference;
                this.Enabled = false;
                this.ExtensionRefCount = 0;
            }
        }
        
        private bool DisplayPresetIcons
        {
            get
            {
                string displayPresetIcons = PathHelpers.FileConverterRegistryKey.GetValue("DisplayPresetIcons") as string;
                if (displayPresetIcons == null)
                {
                    return false;
                }

                if (!bool.TryParse(displayPresetIcons, out bool value))
                {
                    return false;
                }

                return value;
            }
        }

        private PresetReference[] PresetReferences
        {
            get
            {
                this.LoadExtensionSettingsIfNecessary();

                return this.presetReferences;
            }
        }

        protected override bool CanShowMenu()
        {
            this.RefreshExtensionCacheFromSelectedItems();

            PresetReference[] presets = this.PresetReferences;
            foreach (string extension in this.extensionCache)
            {
                foreach (PresetReference presetReference in presets)
                {
                    if (presetReference.InputTypes.Contains(extension))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            this.RefreshPresetList();

            bool displayPresetIcons = this.DisplayPresetIcons;

            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem fileConverterItem = new ToolStripMenuItem
            {
                Text = "File Converter",
                Image = new Icon(Properties.Resources.ApplicationIcon, SystemInformation.SmallIconSize).ToBitmap(),
            };

            foreach (MenuEntry menuEntry in this.menuEntries)
            {
                ToolStripMenuItem root = fileConverterItem;
                if (menuEntry.PresetReference.Folders != null)
                {
                    foreach (string folder in menuEntry.PresetReference.Folders)
                    {
                        ToolStripItem[] folderItems = root.DropDownItems.Find(folder, false);
                        if (folderItems.Length == 0)
                        {
                            ToolStripMenuItem folderItem = new ToolStripMenuItem
                            {
                                Name = folder,
                                Text = folder,
                                Image = new Icon(Properties.Resources.FolderIcon, SystemInformation.SmallIconSize).ToBitmap(),
                            };

                            root.DropDownItems.Add(folderItem);
                            root = folderItem;
                        }
                        else
                        {
                            root = folderItems[0] as ToolStripMenuItem;
                        }

                        if (root == null)
                        {
                            break;
                        }
                    }
                }

                if (root == null)
                {
                    // Fallback when something went wrong during folder creation.
                    root = fileConverterItem;
                }

                ToolStripMenuItem subItem = new ToolStripMenuItem
                {
                    Text = menuEntry.PresetReference.Name,
                    Enabled = menuEntry.Enabled
                };

                if (displayPresetIcons)
                {
                    subItem.Image = new Icon(Properties.Resources.PresetIcon, SystemInformation.SmallIconSize).ToBitmap();
                }

                root.DropDownItems.Add(subItem);
                subItem.Click += (sender, args) => this.ConvertFiles(menuEntry.PresetReference.FullName);
            }

            if (this.menuEntries.Count > 0)
            {
                fileConverterItem.DropDownItems.Add(new ToolStripSeparator());
            }

            {
                ToolStripMenuItem subItem = new ToolStripMenuItem
                {
                    Text = "Configure presets...",
                    Image = new Icon(Properties.Resources.SettingsIcon, SystemInformation.SmallIconSize).ToBitmap(),
                };

                fileConverterItem.DropDownItems.Add(subItem);
                subItem.Click += (sender, args) => this.OpenSettings();
            }

            menu.Items.Add(fileConverterItem);

            return menu;
        }

        private void RefreshExtensionCacheFromSelectedItems()
        {
            // Retrieve selected files extensions.
            this.extensionCache.Clear();
            foreach (string filePath in this.SelectedItemPaths)
            {
                string extension = Path.GetExtension(filePath);
                if (string.IsNullOrEmpty(extension))
                {
                    continue;
                }

                extension = extension.Substring(1).ToLowerInvariant();

                this.extensionCache.Add(extension);
            }
        }

        private void RefreshPresetList()
        {
            this.RefreshExtensionCacheFromSelectedItems();

            // Activate compatible menu entries.
            PresetReference[] presets = this.presetReferences;
            this.menuEntries.Clear();
            foreach (string extension in this.extensionCache)
            {
                foreach (PresetReference presetReference in presets)
                {
                    if (!presetReference.InputTypes.Contains(extension))
                    {
                        continue;
                    }

                    MenuEntry menuEntry = this.menuEntries.Find(entry => entry.PresetReference.FullName == presetReference.FullName);
                    if (menuEntry == null)
                    {
                        menuEntry = new MenuEntry(presetReference);
                        this.menuEntries.Add(menuEntry);
                    }

                    menuEntry.ExtensionRefCount++;
                }
            }

            // Enable presets compatible with all input files.
            foreach (MenuEntry menuEntry in this.menuEntries)
            {
                menuEntry.Enabled = menuEntry.ExtensionRefCount == this.extensionCache.Count;
            }
        }

        private void LoadExtensionSettingsIfNecessary()
        {
            if (this.presetReferences != null)
            {
                return;
            }

            if (File.Exists(PathHelpers.UserSettingsFilePath))
            {
                try
                {
                    XmlHelpers.LoadFromFile("Settings", PathHelpers.UserSettingsFilePath, out this.presetReferences);
                    return;
                }
                catch
                {
                    // Can't handle this error in the explorer extension.
                }
            }

            try
            {
                XmlHelpers.LoadFromFile("Settings", PathHelpers.DefaultSettingsFilePath, out this.presetReferences);
            }
            catch
            {
                // Can't handle this error in the explorer extension.
            }
        }

        private void OpenSettings()
        {
            if (string.IsNullOrEmpty(PathHelpers.FileConverterPath))
            {
                MessageBox.Show("Can't retrieve the file converter executable path. You should try to reinstall the application.");
                return;
            }

            if (!File.Exists(PathHelpers.FileConverterPath))
            {
                MessageBox.Show($"Can't find the file converter executable ({PathHelpers.FileConverterPath}). You should try to reinstall the application.");
                return;
            }

            ProcessStartInfo processStartInfo = new ProcessStartInfo(PathHelpers.FileConverterPath)
            {
                CreateNoWindow = false, 
                UseShellExecute = false, 
                RedirectStandardOutput = false,
            };

            // Build arguments string.
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("--settings");
            
            processStartInfo.Arguments = stringBuilder.ToString();
            Process exeProcess = Process.Start(processStartInfo);
        }

        private void ConvertFiles(string presetName)
        {
            if (string.IsNullOrEmpty(PathHelpers.FileConverterPath))
            {
                MessageBox.Show("Can't retrieve the file converter executable path. You should try to reinstall the application.");
                return;
            }

            if (!File.Exists(PathHelpers.FileConverterPath))
            {
                MessageBox.Show($"Can't find the file converter executable ({PathHelpers.FileConverterPath}). You should try to reinstall the application.");
                return;
            }

            ProcessStartInfo processStartInfo = new ProcessStartInfo(PathHelpers.FileConverterPath)
            {
                CreateNoWindow = false, 
                UseShellExecute = false, 
                RedirectStandardOutput = false,
            };

            // Build arguments string.
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("--conversion-preset ");
            stringBuilder.Append(" \"");
            stringBuilder.Append(presetName);
            stringBuilder.Append("\"");

            foreach (var filePath in this.SelectedItemPaths)
            {
                stringBuilder.Append(" \"");
                stringBuilder.Append(filePath);
                stringBuilder.Append("\"");
            }

            processStartInfo.Arguments = stringBuilder.ToString();
            Process exeProcess = Process.Start(processStartInfo);
        }
    }
}
