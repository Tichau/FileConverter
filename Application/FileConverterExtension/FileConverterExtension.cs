// <copyright file="FileConverterExtension.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverterExtension
{
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
                }
            }

            return false;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem fileConverterItem = new ToolStripMenuItem
            {
                Text = "File Converter"
            };

            {
                ToolStripMenuItem subItem = new ToolStripMenuItem
                                                {
                                                    Text = "To Ogg",
                                                };

                fileConverterItem.DropDownItems.Add(subItem);
                subItem.Click += (sender, args) => this.ConvertFiles(FileType.Ogg);
            }

            {
                ToolStripMenuItem subItem = new ToolStripMenuItem
                {
                    Text = "To Mp3",
                };

                fileConverterItem.DropDownItems.Add(subItem);
                subItem.Click += (sender, args) => this.ConvertFiles(FileType.Mp3);
            }

            {
                ToolStripMenuItem subItem = new ToolStripMenuItem
                {
                    Text = "To Flac",
                };

                fileConverterItem.DropDownItems.Add(subItem);
                subItem.Click += (sender, args) => this.ConvertFiles(FileType.Flac);
            }

            menu.Items.Add(fileConverterItem);

            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\FileConverter");
            this.fileConverterPath = null;
            if (registryKey != null)
            {
                this.fileConverterPath = registryKey.GetValue("Path") as string;
            }

            return menu;
        }

        private void ConvertFiles(FileType ouputType)
        {
            this.fileConverterPath = @"D:\Projects\FileConverter\FileConverter\Application\FileConverter\bin\Debug\FileConverter.exe";
            if (string.IsNullOrEmpty(this.fileConverterPath))
            {
                MessageBox.Show(string.Format("Can't retrieve the file converter executable path. You should try to reinstall the application."));
                return;
            }

            if (!File.Exists(this.fileConverterPath))
            {
                MessageBox.Show(string.Format("Can't find the file converter executable ({0}). You should try to reinstall the application.", this.fileConverterPath));
                return;
            }

            ProcessStartInfo processStartInfo = new ProcessStartInfo(this.fileConverterPath);

            processStartInfo.CreateNoWindow = false;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = false;

            // Build arguments string.
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("--output-type ");
            stringBuilder.Append(ouputType.ToString());

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
