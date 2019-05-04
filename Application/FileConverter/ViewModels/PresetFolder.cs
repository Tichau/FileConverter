// <copyright file="PresetFolder.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using System;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;

namespace FileConverter.ViewModels
{
    public class PresetFolder : ObservableObject
    {
        private string name;
        private ObservableCollection<ObservableObject> children = new ObservableCollection<ObservableObject>();

        public PresetFolder(string name, string[] parents)
        {
            this.Name = name;
            this.ParentFoldersNames = parents;
        }

#if DEBUG
        public PresetFolder()
        {
        }
#endif

        public string Name
        {
            get => this.name;

            set
            {
                this.name = value;

                this.RaisePropertyChanged();
            }
        }

        public string[] ParentFoldersNames
        {
            get;
            private set;
        }

        public string[] FoldersNames
        {
            get
            {
                string[] folders;
                if (this.ParentFoldersNames == null)
                {
                    if (this.name == null)
                    {
                        // Root folder have no name.
                        return null;
                    }

                    folders = new string[1];
                }
                else
                {
                    Diagnostics.Debug.Assert(this.name != null, "Preset folder name should not be null.");

                    folders = new string[this.ParentFoldersNames.Length + 1];
                    System.Array.Copy(this.ParentFoldersNames, folders, this.ParentFoldersNames.Length);
                }

                folders[folders.Length - 1] = this.name;
                return folders;
            }
        }

        public ObservableCollection<ObservableObject> Children
        {
            get => this.children;

            set
            {
                this.children = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsNodeInHierarchy(ObservableObject node, bool recurse)
        {
            Diagnostics.Debug.Assert(node != null, "node != null");
            foreach (ObservableObject child in this.Children)
            {
                if (child == node)
                {
                    return true;
                }

                if (recurse && child is PresetFolder subFolder)
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
