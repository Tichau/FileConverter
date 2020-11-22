// <copyright file="IconButton.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public class IconButton : Button
    {
        public static readonly DependencyProperty IconSourceProperty = DependencyProperty.Register("IconSource", typeof(ImageSource), typeof(IconButton));
        public static readonly DependencyProperty MouseOverBrushProperty = DependencyProperty.Register("MouseOverBrush", typeof(Brush), typeof(IconButton));
        public static readonly DependencyProperty PressedBrushProperty = DependencyProperty.Register("PressedBrush", typeof(Brush), typeof(IconButton));

        static IconButton()
        {
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(IconButton), new FrameworkPropertyMetadata(typeof(IconButton)));
        }

        public ImageSource IconSource
        {
            get => (ImageSource)this.GetValue(IconButton.IconSourceProperty);
            set => this.SetValue(IconButton.IconSourceProperty, value);
        }

        public Brush MouseOverBrush
        {
            get => (Brush)this.GetValue(IconButton.MouseOverBrushProperty);
            set => this.SetValue(IconButton.MouseOverBrushProperty, value);
        }

        public Brush PressedBrush
        {
            get => (Brush)this.GetValue(IconButton.PressedBrushProperty);
            set => this.SetValue(IconButton.PressedBrushProperty, value);
        }
    }
}
