// <copyright file="FileConverterExtension.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverterExtension
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Forms;

    using Microsoft.Win32;

    using SharpShell.Attributes;
    using SharpShell.SharpContextMenu;

    /// <summary>
    /// File converter context menu extension class.
    /// </summary>
    [ComVisible(true), Guid("AF9B72B5-F4E4-44B0-A3D9-B55B748EFE90")]
    [COMServerAssociation(AssociationType.AllFiles)]
    public class FileConverterExtension : SharpContextMenu
    {
        private string fileConverterPath;
        private RegistryKey fileConverterRegistryKey;
        private List<PresetDefinition> presetList = new List<PresetDefinition>();
        private List<string> compatibleInputExtensions = new List<string>(); 

        private RegistryKey FileConverterRegistryKey
        {
            get
            {
                if (this.fileConverterRegistryKey == null)
                {
                    this.fileConverterRegistryKey = Registry.CurrentUser.OpenSubKey(@"Software\FileConverter");
                    if (this.fileConverterRegistryKey == null)
                    {
                        throw new Exception("Can't retrieve file converter registry entry.");
                    }
                }

                return this.fileConverterRegistryKey;
            }
        }

        private string FileConverterPath
        {
            get
            {
                if (string.IsNullOrEmpty(this.fileConverterPath))
                {
                    this.fileConverterPath = this.FileConverterRegistryKey.GetValue("Path") as string;
                }

                return this.fileConverterPath;
            }
        }

        private bool DisplayPresetIcons
        {
            get
            {
                string displayPresetIcons = this.FileConverterRegistryKey.GetValue("DisplayPresetIcons") as string;
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

        private IEnumerable<string> CompatibleInputExtensions
        {
            get
            {
                if (this.compatibleInputExtensions != null && this.compatibleInputExtensions.Count > 0)
                {
                    return this.compatibleInputExtensions;
                }

                this.compatibleInputExtensions.Clear();
                string registryValue = this.FileConverterRegistryKey.GetValue("CompatibleInputExtensions") as string;
                string[] extensions = registryValue.Split(';');
                for (int index = 0; index < extensions.Length; index++)
                {
                    this.compatibleInputExtensions.Add(extensions[index]);
                }

                return this.compatibleInputExtensions;
            }
        } 

        protected override bool CanShowMenu()
        {
            foreach (string filePath in this.SelectedItemPaths)
            {
                string extension = Path.GetExtension(filePath);
                if (string.IsNullOrEmpty(extension))
                {
                    continue;
                }

                extension = extension.Substring(1).ToLowerInvariant();
                if (this.CompatibleInputExtensions.Contains(extension))
                {
                    return true;
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
                Text = "File Converter!",
                Image = new Icon(Properties.Resources.ApplicationIcon, SystemInformation.SmallIconSize).ToBitmap(),
            };

            foreach (PresetDefinition preset in this.presetList)
            {
                ToolStripMenuItem root = fileConverterItem;
                if (preset.Folders != null)
                {
                    foreach (string folder in preset.Folders)
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
                    Text = preset.Name,
                    Enabled = preset.Enabled
                };

                if (displayPresetIcons)
                {
                    subItem.Image = new Icon(Properties.Resources.PresetIcon, SystemInformation.SmallIconSize).ToBitmap();
                }

                root.DropDownItems.Add(subItem);
                subItem.Click += (sender, args) => this.ConvertFiles(preset.FullName);
            }

            if (this.presetList.Count > 0)
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

        private void RefreshPresetList()
        {
            // Retrieve selected files extensions.
            List<string> extensions = new List<string>();
            foreach (string filePath in this.SelectedItemPaths)
            {
                string extension = Path.GetExtension(filePath);
                if (string.IsNullOrEmpty(extension))
                {
                    continue;
                }

                extension = extension.Substring(1).ToLowerInvariant();
                if (extensions.Contains(extension))
                {
                    continue;
                }

                extensions.Add(extension);
            }

            // Compute preset list.
            this.presetList.Clear();
            RegistryKey fileConverterKey = this.FileConverterRegistryKey;
            for (int extensionIndex = 0; extensionIndex < extensions.Count; extensionIndex++)
            {
                string extension = extensions[extensionIndex];
                RegistryKey extensionKey = fileConverterKey.OpenSubKey(extension);
                if (extensionKey == null)
                {
                    continue;
                }

                string presetsString = extensionKey.GetValue("Presets") as string;
                if (presetsString == null)
                {
                    continue;
                }

                string[] presets = presetsString.Split(';');
                
                for (int presetIndex = 0; presetIndex < presets.Length; presetIndex++)
                {
                    string presetName = presets[presetIndex];
                    PresetDefinition presetDefinition = this.presetList.FirstOrDefault(match => match.FullName == presetName);
                    if (presetDefinition == null)
                    {
                        string[] folders = presetName.Split('/');
                        if (folders.Length == 0)
                        {
                            continue;
                        }

                        string name = folders[folders.Length - 1];
                        Array.Resize(ref folders, folders.Length - 1);

                        presetDefinition = new PresetDefinition(presetName, name, folders);
                        this.presetList.Add(presetDefinition);
                    }

                    presetDefinition.ExtensionRefCount++;
                }
            }

            // Update enable states.
            for (int index = 0; index < this.presetList.Count; index++)
            {
                this.presetList[index].Enabled = this.presetList[index].ExtensionRefCount == extensions.Count;
            }
        }

        private void OpenSettings()
        {
            if (string.IsNullOrEmpty(this.FileConverterPath))
            {
                MessageBox.Show(string.Format("Can't retrieve the file converter executable path. You should try to reinstall the application."));
                return;
            }

            if (!File.Exists(this.FileConverterPath))
            {
                MessageBox.Show(string.Format("Can't find the file converter executable ({0}). You should try to reinstall the application.", this.FileConverterPath));
                return;
            }

            ProcessStartInfo processStartInfo = new ProcessStartInfo(this.FileConverterPath);

            processStartInfo.CreateNoWindow = false;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = false;

            // Build arguments string.
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("--settings");
            
            processStartInfo.Arguments = stringBuilder.ToString();
            Process exeProcess = Process.Start(processStartInfo);
        }

        private void ConvertFiles(string presetName)
        {
            if (string.IsNullOrEmpty(this.FileConverterPath))
            {
                MessageBox.Show(string.Format("Can't retrieve the file converter executable path. You should try to reinstall the application."));
                return;
            }

            if (!File.Exists(this.FileConverterPath))
            {
                MessageBox.Show(string.Format("Can't find the file converter executable ({0}). You should try to reinstall the application.", this.FileConverterPath));
                return;
            }

            ProcessStartInfo processStartInfo = new ProcessStartInfo(this.FileConverterPath);

            processStartInfo.CreateNoWindow = false;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = false;

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
