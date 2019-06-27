// <copyright file="SettingsWindow.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Views
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;

    using FileConverter.ViewModels;

    using GalaSoft.MvvmLight.Messaging;

    /// <summary>
    /// Interaction logic for Settings.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private Point lastMouseDown;
        private AbstractTreeNode draggedItem;
        private AbstractTreeNode target;
        private TreeViewItem highlightedItem;

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
            if (this.DragInProgress(args.GetPosition(this.PresetTreeView)))
            {
                if (this.highlightedItem != null)
                {
                    this.highlightedItem.BorderThickness = new Thickness(0);
                }

                // Verify that this is a valid drop and then store the drop target
                TreeViewItem item = this.GetNearestContainer(args.OriginalSource as UIElement);
                if (this.CheckDropTarget(this.draggedItem, item.DataContext as AbstractTreeNode))
                {
                    args.Effects = DragDropEffects.Move;

                    this.highlightedItem = item;
                    if (item.DataContext is PresetFolderNode)
                    {
                        item.BorderThickness = new Thickness(0, 0, 0, 2);
                    }
                    else
                    {
                        item.BorderThickness = new Thickness(0, 2, 0, 0);
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
            TreeViewItem item = this.GetNearestContainer(args.OriginalSource as UIElement);
            if (this.draggedItem != null && item.DataContext is AbstractTreeNode nodeItem)
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
            return sourceItem != targetItem;
        }

        private void MoveItem(AbstractTreeNode sourceItem, AbstractTreeNode targetItem)
        {
            if (targetItem is PresetFolderNode newParent)
            {
                sourceItem.Parent.Children.Remove(sourceItem);
                newParent.Children.Insert(0, sourceItem);
                sourceItem.Parent = newParent;
            }
            else if (targetItem is PresetNode nodeUnderMe)
            {
                sourceItem.Parent.Children.Remove(sourceItem);
                int indexOfNode = nodeUnderMe.Parent.Children.IndexOf(nodeUnderMe);
                nodeUnderMe.Parent.Children.Insert(indexOfNode, sourceItem);
                sourceItem.Parent = nodeUnderMe.Parent;
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
