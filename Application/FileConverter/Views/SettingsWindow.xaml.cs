// <copyright file="SettingsWindow.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Views
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;

    using FileConverter.Diagnostics;
    using FileConverter.ViewModels;

    using GalaSoft.MvvmLight.Messaging;

    /// <summary>
    /// Interaction logic for Settings.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private TextBox selectedPresetNameTextBox;
        private TextBox selectedFolderNameTextBox;
        private string focusMessage;
        private bool dragInProgress = false;

        private enum DragDropTargetPosition
        {
            Before,
            In,
            After,
        }

        public SettingsWindow()
        {
            this.InitializeComponent();

            Messenger.Default.Register<string>(this, "DoFocus", this.DoFocus);

            (this.DataContext as SettingsViewModel).PropertyChanged += this.SettingsWindow_PropertyChanged;

            this.PresetTreeView.MouseDown += this.TreeView_MouseDown;
            this.PresetTreeView.MouseUp += this.TreeView_MouseUp;
        }

        private void SettingsWindow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "SelectedItem")
            {
                TreeViewItem selectedItem = this.GetTreeViewItem(this.PresetTreeView, this.PresetTreeView.SelectedItem);
                selectedItem?.BringIntoView();
            }
        }

        public void DoFocus(string message)
        {
            this.focusMessage = message;
            switch (message)
            {
                case "PresetName":
                    this.selectedPresetNameTextBox?.Focus();
                    this.selectedPresetNameTextBox?.SelectAll();
                    break;

                case "FolderName":
                    this.selectedFolderNameTextBox?.Focus();
                    this.selectedFolderNameTextBox?.SelectAll();
                    break;
            }
        }

        private void TreeView_MouseDown(object sender, MouseButtonEventArgs args)
        {
            FrameworkElement element = args.OriginalSource as FrameworkElement;
            TreeViewItem treeViewItem = this.GetNearestContainer(element);
            if (treeViewItem != null)
            {
                treeViewItem.IsSelected = true;
                this.dragInProgress = args.ChangedButton == MouseButton.Left;
            }
            else
            {
                this.dragInProgress = false;
            }
        }

        private void TreeView_MouseUp(object sender, MouseButtonEventArgs args)
        {
            this.dragInProgress = false;
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs args)
        {
            if (this.dragInProgress &&
                args.LeftButton == MouseButtonState.Pressed && 
                this.PresetTreeView.SelectedItem is AbstractTreeNode nodeToDrag)
            {
                var dataObj = new DataObject();
                dataObj.SetData("DragSource", nodeToDrag);

                DragDropEffects dropEffect = DragDrop.DoDragDrop(this.PresetTreeView, dataObj, DragDropEffects.Move);
                if (dropEffect == DragDropEffects.Move)
                {
                    TreeViewItem draggedItemParent = this.GetTreeViewItem(this.PresetTreeView, nodeToDrag.Parent);
                    if (draggedItemParent != null)
                    {
                        draggedItemParent.IsExpanded = true;
                    }

                    TreeViewItem draggedItem = this.GetTreeViewItem(this.PresetTreeView, nodeToDrag);
                    Debug.Assert(draggedItem != null, "draggedItem != null");
                    draggedItem.IsSelected = true;
                }

                if (dataObj.GetData("HighlightedItem") is TreeViewItem highlightedItem)
                {
                    highlightedItem.BorderThickness = new Thickness(0);
                }
            }
        }

        private void TreeView_DragOver(object sender, DragEventArgs args)
        {
            args.Effects = DragDropEffects.None;
            args.Handled = true;

            if (args.Data.GetData("HighlightedItem") is TreeViewItem highlightedItem)
            {
                highlightedItem.BorderThickness = new Thickness(0);
            }

            if (this.TryComputeDropTarget(args, out AbstractTreeNode target, out DragDropTargetPosition position))
            {
                args.Effects = DragDropEffects.Move;
                args.Handled = true;

                FrameworkElement element = args.OriginalSource as FrameworkElement;
                TreeViewItem item = this.GetNearestContainer(element);

                args.Data.SetData("HighlightedItem", item);

                switch (position)
                {
                    case DragDropTargetPosition.Before:
                        item.BorderThickness = new Thickness(0, 2, 0, 0);
                        break;

                    case DragDropTargetPosition.In:
                        item.BorderThickness = new Thickness(2, 2, 2, 2);
                        break;

                    case DragDropTargetPosition.After:
                        item.BorderThickness = new Thickness(0, 0, 0, 2);
                        break;
                }
            }
        }

        private void TreeView_Drop(object sender, DragEventArgs args)
        {
            args.Effects = DragDropEffects.None;
            args.Handled = true;

            if (this.TryComputeDropTarget(args, out AbstractTreeNode target, out DragDropTargetPosition position))
            {
                AbstractTreeNode nodeToDrag = (AbstractTreeNode)args.Data.GetData("DragSource");
                Debug.Assert(nodeToDrag != null, "nodeToDrag != null");

                this.MoveItem(nodeToDrag, target, position);

                args.Effects = DragDropEffects.Move;
            }
        }

        private bool TryComputeDropTarget(DragEventArgs args, out AbstractTreeNode target, out DragDropTargetPosition position)
        {
            FrameworkElement source = args.OriginalSource as FrameworkElement;
            TreeViewItem item = this.GetNearestContainer(source);
            
            target = item?.DataContext as AbstractTreeNode;

            Debug.Assert(source != null, "source should not be null");
            Point relativePosition = args.GetPosition(source);

            if (target is PresetFolderNode)
            {
                if (item.IsExpanded)
                {
                    if (relativePosition.Y - (source.ActualHeight / 2) < 0)
                    {
                        position = DragDropTargetPosition.Before;
                    }
                    else
                    {
                        position = DragDropTargetPosition.In;
                    }
                }
                else
                {
                    if (relativePosition.Y < source.ActualHeight / 3)
                    {
                        position = DragDropTargetPosition.Before;
                    }
                    else if (relativePosition.Y < 2 * (source.ActualHeight / 3))
                    {
                        position = DragDropTargetPosition.In;
                    }
                    else
                    {
                        position = DragDropTargetPosition.After;
                    }
                }
            }
            else
            {
                if (relativePosition.Y - (source.ActualHeight / 2) < 0)
                {
                    position = DragDropTargetPosition.Before;
                }
                else
                {
                    position = DragDropTargetPosition.After;
                }
            }

            AbstractTreeNode nodeToDrag = (AbstractTreeNode)args.Data.GetData("DragSource");
            Debug.Assert(nodeToDrag != null, "nodeToDrag != null");

            return target != null && this.CheckDropTarget(nodeToDrag, target);
        }

        private bool CheckDropTarget(AbstractTreeNode sourceItem, AbstractTreeNode targetItem)
        {
            // Check whether the target item is meeting your condition
            if (targetItem == null)
            {
                return false;
            }

            AbstractTreeNode folder = targetItem;
            while (folder != null)
            {
                if (folder == sourceItem)
                {
                    return false;
                }

                folder = folder.Parent;
            }

            return true;
        }

        private void MoveItem(AbstractTreeNode sourceItem, AbstractTreeNode targetItem, DragDropTargetPosition targetPosition)
        {
            switch (targetPosition)
            {
                case DragDropTargetPosition.Before:
                    {
                        sourceItem.Parent.Children.Remove(sourceItem);
                        int indexOfNode = targetItem.Parent.Children.IndexOf(targetItem);
                        targetItem.Parent.Children.Insert(indexOfNode, sourceItem);
                        sourceItem.Parent = targetItem.Parent;
                    }

                    break;

                case DragDropTargetPosition.In:
                    {
                        if (targetItem is PresetFolderNode newParent)
                        {
                            sourceItem.Parent.Children.Remove(sourceItem);
                            newParent.Children.Insert(0, sourceItem);
                            sourceItem.Parent = newParent;
                        }
                        else
                        {
                            Debug.LogError("Can move element in target.");
                        }
                    }

                    break;

                case DragDropTargetPosition.After:
                    {
                        sourceItem.Parent.Children.Remove(sourceItem);
                        int indexOfNode = targetItem.Parent.Children.IndexOf(targetItem);
                        targetItem.Parent.Children.Insert(indexOfNode + 1, sourceItem);
                        sourceItem.Parent = targetItem.Parent;
                    }

                    break;
            }
        }

        private TreeViewItem GetNearestContainer(UIElement element)
        {
            while (element is not TreeViewItem container&& element != null)
            {
                element = VisualTreeHelper.GetParent(element) as UIElement;
                container = element as TreeViewItem;
            }

            return container;
        }

        /// <summary>
        /// Recursively search for an item in this subtree.
        /// </summary>
        /// <param name="container">The parent ItemsControl. This can be a TreeView or a TreeViewItem.</param>
        /// <param name="item">The item to search for.</param>
        /// <returns>The TreeViewItem that contains the specified item.</returns>
        /// Source: https://docs.microsoft.com/fr-fr/dotnet/framework/wpf/controls/how-to-find-a-treeviewitem-in-a-treeview
        private TreeViewItem GetTreeViewItem(ItemsControl container, object item)
        {
            if (container != null)
            {
                if (container.DataContext == item)
                {
                    return container as TreeViewItem;
                }

                for (int i = 0, count = container.Items.Count; i < count; i++)
                {
                    TreeViewItem subContainer = (TreeViewItem)container.ItemContainerGenerator.ContainerFromIndex(i);
                    if (subContainer != null)
                    {
                        // Search the next level for the object.
                        TreeViewItem resultContainer = this.GetTreeViewItem(subContainer, item);
                        if (resultContainer != null)
                        {
                            return resultContainer;
                        }
                    }
                }
            }

            return null;
        }

        private void SelectedPresetName_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.selectedPresetNameTextBox = (TextBox)sender;
            if (this.focusMessage == "PresetName")
            {
                this.DoFocus(this.focusMessage);
                this.focusMessage = null;
            }
        }

        private void SelectedFolderName_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.selectedFolderNameTextBox = (TextBox)sender;
            if (this.focusMessage == "FolderName")
            {
                this.DoFocus(this.focusMessage);
                this.focusMessage = null;
            }
        }
    }
}
