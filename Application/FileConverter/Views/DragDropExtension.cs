// <copyright file="DragDropExtension.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FileConverter.Views
{
    /// <summary>
    /// Provides extended support for drag drop operation
    /// </summary>

    public static class DragDropExtension
    {
        public static readonly DependencyProperty ScrollOnDragDropProperty = DependencyProperty.RegisterAttached("ScrollOnDragDrop", typeof(bool), typeof(DragDropExtension), new PropertyMetadata(false, HandleScrollOnDragDropChanged));

        public static bool GetScrollOnDragDrop(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return (bool)element.GetValue(ScrollOnDragDropProperty);
        }

        public static void SetScrollOnDragDrop(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(ScrollOnDragDropProperty, value);
        }

        private static void HandleScrollOnDragDropChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            FrameworkElement container = dependencyObject as FrameworkElement;

            if (dependencyObject == null)
            {
                Diagnostics.Debug.LogError("Invalid types!");
                return;
            }

            Unsubscribe(container);

            if (true.Equals(args.NewValue))
            {
                Subscribe(container);
            }
        }
        
        private static void Subscribe(FrameworkElement container)
        {
            container.PreviewDragOver += OnContainerPreviewDragOver;
        }
        
        private static void OnContainerPreviewDragOver(object sender, DragEventArgs args)
        {
            const double Tolerance = 60;
            const double Offset = 20;

            FrameworkElement container = sender as FrameworkElement;
            if (container == null)
            {
                return;
            }

            ScrollViewer scrollViewer = GetFirstVisualChild<ScrollViewer>(container);
            if (scrollViewer == null)
            {
                return;
            }

            double verticalPos = args.GetPosition(container).Y;

            if (verticalPos < Tolerance) // Top of visible list? 
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - Offset); //Scroll up. 
            }
            else if (verticalPos > container.ActualHeight - Tolerance) // Bottom of visible list? 
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + Offset); //Scroll down.     
            }
        }

        private static void Unsubscribe(FrameworkElement container)
        {
            container.PreviewDragOver -= OnContainerPreviewDragOver;
        }
        
        public static T GetFirstVisualChild<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            if (dependencyObject != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);
                    if (child is T visualChild)
                    {
                        return visualChild;
                    }
                    
                    T childItem = GetFirstVisualChild<T>(child);
                    if (childItem != null)
                    {
                        return childItem;
                    }
                }
            }

            return null;
        }
    }
}
