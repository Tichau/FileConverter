// <copyright file="FileConverterExtension.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverterExtension
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
                switch (extension)
                {
                    case "ape":
                    case "mp3":
                    case "wav":
                    case "ogg":
                    case "flac":
                    case "wma":
                        return true;
                }
            }

            return false;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            this.RefreshPresetList();

            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem fileConverterItem = new ToolStripMenuItem
            {
                Text = "File Converter",
                Image = Properties.Resources.ApplicationIcon_16x16.ToBitmap(),
            };

            for (int index = 0; index < this.presetList.Count; index++)
            {
                PresetDefinition preset = this.presetList[index];

                ToolStripMenuItem subItem = new ToolStripMenuItem
                {
                    Text = preset.Name,
                    Enabled = preset.Enabled
                };

                fileConverterItem.DropDownItems.Add(subItem);
                subItem.Click += (sender, args) => this.ConvertFiles(preset.Name);
            }

            if (this.presetList.Count > 0)
            {
                fileConverterItem.DropDownItems.Add(new ToolStripSeparator());
            }

            {
                ToolStripMenuItem subItem = new ToolStripMenuItem
                {
                    Text = "Settings",
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
                    PresetDefinition presetDefinition = this.presetList.FirstOrDefault(match => match.Name == presetName);
                    if (presetDefinition == null)
                    {
                        presetDefinition = new PresetDefinition(presetName);
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
