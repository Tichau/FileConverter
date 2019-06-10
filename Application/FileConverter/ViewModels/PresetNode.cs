// <copyright file="PresetNode.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using System;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;

namespace FileConverter.ViewModels
{
    public abstract class AbstractTreeNode : ObservableObject
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
