// <copyright file="PresetNode.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using GalaSoft.MvvmLight;

namespace FileConverter.ViewModels
{
    public abstract class AbstractTreeNode : ObservableObject, IDataErrorInfo
    {
        private PresetFolderNode parent;

        protected AbstractTreeNode(PresetFolderNode parent)
        {
            this.Parent = parent;
        }

#if DEBUG
        protected AbstractTreeNode()
        {
        }
#endif

        public PresetFolderNode Parent
        {
            get => this.parent;
            set
            {
                this.parent = value;
                this.RaisePropertyChanged();
            }
        }

        public abstract string Name
        {
            get;
            set;
        }

        public string this[string columnName] => this.Validate(columnName);

        public string Error
        {
            get
            {
                if (this.Parent == null)
                {
                    // This is the root folder. Don't check rules on this specific folder.
                    return string.Empty;
                }

                string errorString = this.Validate("Name");
                if (!string.IsNullOrEmpty(errorString))
                {
                    return errorString;
                }

                errorString = this.Validate("OutputFileNameTemplate");
                if (!string.IsNullOrEmpty(errorString))
                {
                    return errorString;
                }

                return string.Empty;
            }
        }

        protected virtual string Validate(string propertyName)
        {
            // Return error message if there is an error, else return empty or null string.
            switch (propertyName)
            {
                case "Name":
                {
                    if (string.IsNullOrEmpty(this.Name))
                    {
                        return "The preset name can't be empty.";
                    }

                    if (this.Name.Contains(";"))
                    {
                        return "The preset name can't contains the character ';'.";
                    }

                    if (this.Name.Contains("/"))
                    {
                        return "The preset name can't contains the character '/'.";
                    }

                    if (this.Parent != null)
                    {
                        int count = this.Parent.Children.Count(node => node.Name == this.Name);
                        if (count > 1)
                        {
                            return "The preset name is already used.";
                        }
                    }
                }

                break;
            }

            return string.Empty;
        }
    }

    public class PresetNode : AbstractTreeNode
    {
        public PresetNode(ConversionPreset preset, PresetFolderNode parent) : base(parent)
        {
            this.Preset = preset;
        }

#if DEBUG
        public PresetNode() : base()
        {
        }
#endif

        public ConversionPreset Preset
        {
            get;
#if DEBUG
            set;
#endif
        }

        public override string Name
        {
            get => this.Preset.ShortName;

            set
            {
                this.Preset.ShortName = value;
                this.RaisePropertyChanged();
            }
        }

        public string OutputFileNameTemplate
        {
            get
            {
                return this.Preset.OutputFileNameTemplate;
            }

            set
            {
                this.Preset.OutputFileNameTemplate = value;
                this.RaisePropertyChanged();
            }
        }

        protected override string Validate(string propertyName)
        {
            string error = base.Validate(propertyName);
            if (!string.IsNullOrEmpty(error))
            {
                return error;
            }

            // Return error message if there is an error, else return empty or null string.
            switch (propertyName)
            {
                case "OutputFileNameTemplate":
                    {
                        string sampleOutputFilePath = this.Preset.GenerateOutputFilePath(FileConverter.Properties.Resources.OutputFileNameTemplateSample, 1, 3);
                        if (string.IsNullOrEmpty(sampleOutputFilePath))
                        {
                            return "The output filename template must produce a non empty result.";
                        }

                        if (!PathHelpers.IsPathValid(sampleOutputFilePath))
                        {
                            // Diagnostic to feedback purpose.
                            // Drive letter.
                            if (!PathHelpers.IsPathDriveLetterValid(sampleOutputFilePath))
                            {
                                return "The output filename template must define a root (for example c:\\, use (p) to use the input file path).";
                            }

                            // File name.
                            string filename = PathHelpers.GetFileName(sampleOutputFilePath);
                            if (filename == null)
                            {
                                return "The output file name must not be empty (use (f) to use the name of the input file).";
                            }

                            char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
                            for (int index = 0; index < invalidFileNameChars.Length; index++)
                            {
                                if (filename.Contains(invalidFileNameChars[index]))
                                {
                                    return "The output file name must not contains the character '" + invalidFileNameChars[index] + "'.";
                                }
                            }

                            // Directory names.
                            string path = sampleOutputFilePath.Substring(3, sampleOutputFilePath.Length - 3 - filename.Length);
                            char[] invalidPathChars = System.IO.Path.GetInvalidPathChars();
                            for (int index = 0; index < invalidPathChars.Length; index++)
                            {
                                if (string.IsNullOrEmpty(path))
                                {
                                    return "The output directory name must not be empty (use (d0), (d1), ... to use the name of the parent directories of the input file).";
                                }

                                if (path.Contains(invalidPathChars[index]))
                                {
                                    return "The output directory name must not contains the character '" + invalidPathChars[index] + "'.";
                                }
                            }

                            string[] directories = path.Split('\\');
                            for (int index = 0; index < directories.Length; ++index)
                            {
                                string directoryName = directories[index];
                                if (string.IsNullOrEmpty(directoryName))
                                {
                                    return "The output directory name must not be empty (use (d0), (d1), ... to use the name of the parent directories of the input file).";
                                }
                            }

                            return "The output filename template is invalid";
                        }
                    }

                    break;
            }

            return string.Empty;
        }
    }

    public class PresetFolderNode : AbstractTreeNode
    {
        private string name;
        private ObservableCollection<AbstractTreeNode> children = new ObservableCollection<AbstractTreeNode>();

        public PresetFolderNode(string name, PresetFolderNode parent) : base(parent)
        {
            this.Name = name;
        }

#if DEBUG
        public PresetFolderNode() : base()
        {
        }
#endif

        public override string Name
        {
            get => this.name;

            set
            {
                this.name = value;
                this.RaisePropertyChanged();
            }
        }

        public ObservableCollection<AbstractTreeNode> Children
        {
            get => this.children;

            set
            {
                this.children = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsNodeInHierarchy(AbstractTreeNode node, bool recurse)
        {
            Diagnostics.Debug.Assert(node != null, "node != null");
            foreach (ObservableObject child in this.Children)
            {
                if (child == node)
                {
                    return true;
                }

                if (recurse && child is PresetFolderNode subFolder)
                {
                    if (subFolder.IsNodeInHierarchy(node, true))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
