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
        private Point lastMouseDown;
        private AbstractTreeNode draggedItem;
        private TreeViewItem highlightedItem;
        private AbstractTreeNode target;
        private DragDropTargetPosition targetPosition;

        private enum DragDropTargetPosition
        {
            Before,
            After,
        }

        public SettingsWindow()
        {
            this.InitializeComponent();

            Messenger.Default.Register<string>(this, "DoFocus", this.DoFocus);
        }

        public void DoFocus(string message)
        {
            switch (message)
            {
                case "PresetName":
                    this.PresetNameTextBox.Focus();
                    this.PresetNameTextBox.SelectAll();
                    break;

                case "FolderName":
                    this.FolderNameTextBox.Focus();
                    this.FolderNameTextBox.SelectAll();
                    break;
            }
        }

        private void OnInputTypeChecked(object sender, RoutedEventArgs e)
        {
            SettingsViewModel dataContext = this.DataContext as SettingsViewModel;
            if (dataContext.SelectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            if (!checkBox.IsVisible)
            {
                return;
            }

            string inputFormat = (checkBox.Content as string).ToLowerInvariant();

            dataContext.SelectedPreset.Preset.AddInputType(inputFormat);
        }

        private void OnInputTypeUnchecked(object sender, RoutedEventArgs e)
        {
            SettingsViewModel dataContext = this.DataContext as SettingsViewModel;
            if (dataContext.SelectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            if (!checkBox.IsVisible)
            {
                return;
            }

            string inputFormat = (checkBox.Content as string).ToLowerInvariant();

            dataContext.SelectedPreset.Preset.RemoveInputType(inputFormat);
        }

        private void OnInputTypeCategoryChecked(object sender, RoutedEventArgs e)
        {
            SettingsViewModel dataContext = this.DataContext as SettingsViewModel;
            if (dataContext.SelectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            string categoryName = checkBox.Content as string;
            InputExtensionCategory category = dataContext.InputCategories.FirstOrDefault(match => match.Name == categoryName);
            if (category == null)
            {
                return;
            }

            foreach (string inputExtension in category.InputExtensionNames)
            {
                dataContext.SelectedPreset.Preset.AddInputType(inputExtension);
            }
        }

        private void OnInputTypeCategoryUnchecked(object sender, RoutedEventArgs e)
        {
            SettingsViewModel dataContext = this.DataContext as SettingsViewModel;
            if (dataContext.SelectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            string categoryName = checkBox.Content as string;
            InputExtensionCategory category = dataContext.InputCategories.FirstOrDefault(match => match.Name == categoryName);
            if (category == null)
            {
                return;
            }

            foreach (string inputExtension in category.InputExtensionNames)
            {
                dataContext.SelectedPreset.Preset.RemoveInputType(inputExtension);
            }
        }

        private void TreeView_DragOver(object sender, DragEventArgs args)
        {
            Point position = args.GetPosition(this.PresetTreeView);
            if (this.DragInProgress(position))
            {
                if (this.highlightedItem != null)
                {
                    this.highlightedItem.BorderThickness = new Thickness(0);
                }

                // Verify that this is a valid drop and then store the drop target
                FrameworkElement element = args.OriginalSource as FrameworkElement;
                TreeViewItem item = this.GetNearestContainer(element);

                if (this.CheckDropTarget(this.draggedItem, item?.DataContext as AbstractTreeNode))
                {
                    Debug.Assert(element != null, "element should not be null");
                    Point relativePosition = args.GetPosition(element);

                    DragDropTargetPosition dragDropTargetPosition;
                    if (relativePosition.Y - (element.ActualHeight / 2) < 0)
                    {
                        dragDropTargetPosition = DragDropTargetPosition.Before;
                    }
                    else
                    {
                        dragDropTargetPosition = DragDropTargetPosition.After;
                    }

                    args.Effects = DragDropEffects.Move;

                    this.highlightedItem = item;
                    this.targetPosition = dragDropTargetPosition;

                    switch (this.targetPosition)
                    {
                        case DragDropTargetPosition.Before:
                            item.BorderThickness = new Thickness(0, 2, 0, 0);
                            break;

                        case DragDropTargetPosition.After:
                            item.BorderThickness = new Thickness(0, 0, 0, 2);
                            break;
                    }
                }
                else
                {
                    args.Effects = DragDropEffects.None;
                }
            }

            args.Handled = true;
        }

        private void TreeView_Drop(object sender, DragEventArgs args)
        {
            args.Effects = DragDropEffects.None;
            args.Handled = true;

            // Verify that this is a valid drop and then store the drop target
            FrameworkElement element = args.OriginalSource as FrameworkElement;
            TreeViewItem item = this.GetNearestContainer(element);
            if (this.draggedItem != null && item != null && item.DataContext is AbstractTreeNode nodeItem)
            {
                this.target = nodeItem;
                args.Effects = DragDropEffects.Move;
            }
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs args)
        {
            if (args.LeftButton == MouseButtonState.Pressed && this.DragInProgress(args.GetPosition(this.PresetTreeView)))
            {
                this.draggedItem = (AbstractTreeNode)this.PresetTreeView.SelectedItem;
                if (this.draggedItem != null)
                {
                    DragDropEffects finalDropEffect = DragDrop.DoDragDrop(this.PresetTreeView, this.PresetTreeView.SelectedValue, DragDropEffects.Move);

                    if (finalDropEffect == DragDropEffects.Move && this.target != null)
                    {
                        // A Move drop was accepted
                        if (this.CheckDropTarget(this.draggedItem, this.target))
                        {
                            this.MoveItem(this.draggedItem, this.target);

                            if (this.highlightedItem != null)
                            {
                                this.highlightedItem.BorderThickness = new Thickness(0);
                            }

                            this.highlightedItem = null;
                            this.target = null;
                            this.draggedItem = null;
                        }
                    }
                }
            }
        }

        private void TreeView_MouseDown(object sender, MouseButtonEventArgs args)
        {
            if (args.ChangedButton == MouseButton.Left)
            {
                this.lastMouseDown = args.GetPosition(this.PresetTreeView);
            }
        }

        private bool DragInProgress(Point currentPosition)
        {
            return Math.Abs(currentPosition.X - this.lastMouseDown.X) > 10.0 ||
                   Math.Abs(currentPosition.Y - this.lastMouseDown.Y) > 10.0;
        }

        private bool CheckDropTarget(AbstractTreeNode sourceItem, AbstractTreeNode targetItem)
        {
            // Check whether the target item is meeting your condition
            return sourceItem != targetItem && targetItem != null;
        }

        private void MoveItem(AbstractTreeNode sourceItem, AbstractTreeNode targetItem)
        {
            switch (this.targetPosition)
            {
                case DragDropTargetPosition.Before:
                    {
                        sourceItem.Parent.Children.Remove(sourceItem);
                        int indexOfNode = targetItem.Parent.Children.IndexOf(targetItem);
                        targetItem.Parent.Children.Insert(indexOfNode, sourceItem);
                        sourceItem.Parent = targetItem.Parent;
                    }

                    break;

                case DragDropTargetPosition.After:
                    {
                        if (targetItem is PresetFolderNode newParent)
                        {
                            sourceItem.Parent.Children.Remove(sourceItem);
                            newParent.Children.Insert(0, sourceItem);
                            sourceItem.Parent = newParent;
                        }
                        else if (targetItem is PresetNode nodeBeforeMe)
                        {
                            sourceItem.Parent.Children.Remove(sourceItem);
                            int indexOfNode = nodeBeforeMe.Parent.Children.IndexOf(nodeBeforeMe);
                            nodeBeforeMe.Parent.Children.Insert(indexOfNode + 1, sourceItem);
                            sourceItem.Parent = nodeBeforeMe.Parent;
                        }
                    }

                    break;
            }
        }

        private TreeViewItem GetNearestContainer(UIElement element)
        {
            // Walk up the element tree to the nearest tree view item.
            TreeViewItem container = element as TreeViewItem;
            while (container == null && element != null)
            {
                element = VisualTreeHelper.GetParent(element) as UIElement;
                container = element as TreeViewItem;
            }

            return container;
        }
    }
}
