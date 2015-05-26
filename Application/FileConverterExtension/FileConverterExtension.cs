// <copyright file="FileConverterExtension.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverterExtension
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Forms;

    using Microsoft.Win32;

    using SharpShell.Attributes;
    using SharpShell.SharpContextMenu;

    [ComVisible(true), Guid("AF9B72B5-F4E4-44B0-A3D9-B55B748EFE90")]
    [COMServerAssociation(AssociationType.AllFiles)]
    public class FileConverterExtension : SharpContextMenu
    {
        private string fileConverterPath;
        private RegistryKey fileConverterRegistryKey;
        private List<string> presetList = new List<string>();

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
                    case "mp3":
                        return true;

                    case "wav":
                        return true;

                    case "ogg":
                        return true;

                    case "flac":
                        return true;

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
                Text = "File Converter"
            };

            for (int index = 0; index < this.presetList.Count; index++)
            {
                string presetName = this.presetList[index];
                ToolStripMenuItem subItem = new ToolStripMenuItem
                {
                    Text = presetName,
                };

                fileConverterItem.DropDownItems.Add(subItem);
                subItem.Click += (sender, args) => this.ConvertFiles(presetName);
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
            this.presetList.Clear();
            RegistryKey fileConverterKey = this.FileConverterRegistryKey;
            foreach (string filePath in this.SelectedItemPaths)
            {
                string extension = Path.GetExtension(filePath);
                if (string.IsNullOrEmpty(extension))
                {
                    continue;
                }

                extension = extension.Substring(1).ToLowerInvariant();

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
                for (int index = 0; index < presets.Length; index++)
                {
                    if (this.presetList.Contains(presets[index]))
                    {
                        continue;
                    }

                    this.presetList.Add(presets[index]);
                }
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
