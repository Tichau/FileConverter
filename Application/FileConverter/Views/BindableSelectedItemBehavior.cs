// Source: https://tyrrrz.me/Blog/WPF-TreeView-SelectedItem-TwoWay-binding

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace FileConverter.Views
{
    public class TreeViewSelectionBehavior : Behavior<TreeView>
    {
        public delegate bool IsChildOfPredicate(object nodeA, object nodeB);

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object),
                typeof(TreeViewSelectionBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedItemChanged));

        public static readonly DependencyProperty HierarchyPredicateProperty =
            DependencyProperty.Register(nameof(HierarchyPredicate), typeof(IsChildOfPredicate),
                typeof(TreeViewSelectionBehavior),
                new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ExpandSelectedProperty =
            DependencyProperty.Register(nameof(ExpandSelected), typeof(bool),
                typeof(TreeViewSelectionBehavior),
                new FrameworkPropertyMetadata(false));

        private readonly EventSetter treeViewItemEventSetter;
        private bool modelHandled;

        public TreeViewSelectionBehavior()
        {
            this.treeViewItemEventSetter = new EventSetter(FrameworkElement.LoadedEvent, new RoutedEventHandler(this.OnTreeViewItemLoaded));
        }

        // Bindable selected item
        public object SelectedItem
        {
            get => this.GetValue(SelectedItemProperty);
            set => this.SetValue(SelectedItemProperty, value);
        }

        // Predicate that checks if two items are hierarchically related
        public IsChildOfPredicate HierarchyPredicate
        {
            get => (IsChildOfPredicate)this.GetValue(HierarchyPredicateProperty);
            set => this.SetValue(HierarchyPredicateProperty, value);
        }

        // Should expand selected?
        public bool ExpandSelected
        {
            get => (bool)this.GetValue(ExpandSelectedProperty);
            set => this.SetValue(ExpandSelectedProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.SelectedItemChanged += this.OnTreeViewSelectedItemChanged;
            ((INotifyCollectionChanged)this.AssociatedObject.Items).CollectionChanged += this.OnTreeViewItemsChanged;

            this.UpdateTreeViewItemStyle();
            this.modelHandled = true;
            this.UpdateAllTreeViewItems();
            this.modelHandled = false;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (this.AssociatedObject != null)
            {
                this.AssociatedObject.ItemContainerStyle?.Setters?.Remove(this.treeViewItemEventSetter);
                this.AssociatedObject.SelectedItemChanged -= this.OnTreeViewSelectedItemChanged;
                ((INotifyCollectionChanged)this.AssociatedObject.Items).CollectionChanged -= this.OnTreeViewItemsChanged;
            }
        }

        private static void OnSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var behavior = (TreeViewSelectionBehavior)sender;
            if (behavior.modelHandled)
            {
                return;
            }

            if (behavior.AssociatedObject == null)
            {
                return;
            }

            behavior.modelHandled = true;
            behavior.UpdateAllTreeViewItems();
            behavior.modelHandled = false;
        }

        // Update state of all items starting with given, with optional recursion
        private void UpdateTreeViewItem(TreeViewItem item, bool recurse)
        {
            if (this.SelectedItem == null)
            {
                return;
            }

            var model = item.DataContext;

            // If the selected item is this model and is not yet selected - select and return
            if (this.SelectedItem == model && !item.IsSelected)
            {
                item.IsSelected = true;
            }
            // If the selected item is a parent of this model - expand
            else
            {
                bool isParentOfModel = this.HierarchyPredicate?.Invoke(this.SelectedItem, model) ?? true;
                if (isParentOfModel)
                {
                    item.IsExpanded = true;
                }
            }

            if (item.IsSelected && this.ExpandSelected)
            {
                item.IsExpanded = true;
            }

            // Recurse into children
            if (recurse)
            {
                foreach (var subitem in item.Items)
                {
                    if (item.ItemContainerGenerator.ContainerFromItem(subitem) is TreeViewItem tvi)
                    {
                        this.UpdateTreeViewItem(tvi, true);
                    }
                }
            }
        }

        // Update state of all items
        private void UpdateAllTreeViewItems()
        {
            var treeView = this.AssociatedObject;
            foreach (var item in treeView.Items)
            {
                if (treeView.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem tvi)
                {
                    this.UpdateTreeViewItem(tvi, true);
                }
            }
        }

        // Inject Loaded event handler into ItemContainerStyle
        private void UpdateTreeViewItemStyle()
        {
            if (this.AssociatedObject.ItemContainerStyle == null)
            {
                this.AssociatedObject.ItemContainerStyle = new Style(
                    typeof(TreeViewItem),
                    Application.Current.TryFindResource(typeof(TreeViewItem)) as Style);
            }

            if (!this.AssociatedObject.ItemContainerStyle.Setters.Contains(this.treeViewItemEventSetter))
            {
                this.AssociatedObject.ItemContainerStyle.Setters.Add(this.treeViewItemEventSetter);
            }
        }

        private void OnTreeViewItemsChanged(object sender,
            NotifyCollectionChangedEventArgs args)
        {
            this.UpdateAllTreeViewItems();
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> args)
        {
            if (this.modelHandled)
            {
                return;
            }
            if (this.AssociatedObject.Items.SourceCollection == null)
            {
                return;
            }

            this.SelectedItem = args.NewValue;
        }

        private void OnTreeViewItemLoaded(object sender, RoutedEventArgs args)
        {
            this.UpdateTreeViewItem((TreeViewItem)sender, false);
        }
    }
}
