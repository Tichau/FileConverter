// <copyright file="EncodingQualitySliderControl.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Interaction logic for EncodingQualitySliderControl.xaml
    /// </summary>
    public partial class EncodingQualitySliderControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty EncodingModeProperty = DependencyProperty.Register(
            "EncodingMode",
            typeof(EncodingMode),
            typeof(EncodingQualitySliderControl),
            null);

        public static readonly DependencyProperty BitrateProperty = DependencyProperty.Register(
            "Bitrate",
            typeof(double),
            typeof(EncodingQualitySliderControl),
            null);

        public event PropertyChangedEventHandler PropertyChanged;

        [Category("Behavior")]
        public event RoutedPropertyChangedEventHandler<double> ValueChanged;
        
        public EncodingQualitySliderControl()
        {
            this.InitializeComponent();

            (this.Content as FrameworkElement).DataContext = this;

            this.Bitrate = this.GetNearestTickValue(this.Bitrate);

            this.slider.ValueChanged += Slider_ValueChanged;

            DependencyPropertyDescriptor encodingModeDescriptor = DependencyPropertyDescriptor.FromProperty(
                EncodingQualitySliderControl.EncodingModeProperty,
                typeof(EncodingQualitySliderControl));

            encodingModeDescriptor.AddValueChanged(this, this.EncodingModeValueChanged);
        }
        
        public EncodingMode EncodingMode
        {
            get
            {
                return (EncodingMode)this.GetValue(EncodingQualitySliderControl.EncodingModeProperty);
            }

            set
            {
                this.SetValueDependencyProperty(EncodingQualitySliderControl.EncodingModeProperty, value);
            }
        }

        public double Bitrate
        {
            get
            {
                return (double)this.GetValue(EncodingQualitySliderControl.BitrateProperty);
            }

            set
            {
                //this.SetValue(EncodingQualitySliderControl.BitrateProperty, value);
                this.SetValueDependencyProperty(EncodingQualitySliderControl.BitrateProperty, value);
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.ValueChanged?.Invoke(this, e);
        }

        private void SetValueDependencyProperty(DependencyProperty dependencyProperty, object value, [CallerMemberName] string propertyName = null)
        {
            this.SetValue(dependencyProperty, value);

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void EncodingModeValueChanged(object sender, EventArgs eventArgs)
        {
            double previousValue = this.Bitrate;

            switch (this.EncodingMode)
            {
                case EncodingMode.VBR:
                    {
                        this.slider.Minimum = 65;
                        this.slider.Maximum = 245;
                        this.slider.SelectionStart = 115;
                        this.slider.SelectionEnd = 245;
                        this.slider.Ticks.Clear();
                        this.slider.Ticks.Add(65);
                        this.slider.Ticks.Add(85);
                        this.slider.Ticks.Add(100);
                        this.slider.Ticks.Add(115);
                        this.slider.Ticks.Add(130);
                        this.slider.Ticks.Add(165);
                        this.slider.Ticks.Add(175);
                        this.slider.Ticks.Add(190);
                        this.slider.Ticks.Add(225);
                        this.slider.Ticks.Add(245);
                        break;
                    }

                case EncodingMode.CBR:
                    {
                        this.slider.Minimum = 8;
                        this.slider.Maximum = 320;
                        this.slider.SelectionStart = 128;
                        this.slider.SelectionEnd = 256;
                        this.slider.Ticks.Clear();
                        this.slider.Ticks.Add(8);
                        this.slider.Ticks.Add(16);
                        this.slider.Ticks.Add(24);
                        this.slider.Ticks.Add(32);
                        this.slider.Ticks.Add(40);
                        this.slider.Ticks.Add(48);
                        this.slider.Ticks.Add(64);
                        this.slider.Ticks.Add(80);
                        this.slider.Ticks.Add(96);
                        this.slider.Ticks.Add(112);
                        this.slider.Ticks.Add(128);
                        this.slider.Ticks.Add(160);
                        this.slider.Ticks.Add(192);
                        this.slider.Ticks.Add(224);
                        this.slider.Ticks.Add(256);
                        this.slider.Ticks.Add(320);
                        break;
                    }
            }

            this.Bitrate = this.GetNearestTickValue(previousValue);
        }

        private double GetNearestTickValue(double value)
        {
            double minimumDifference = float.PositiveInfinity;
            double nearestTick = value;
            for (int index = 0; index < this.slider.Ticks.Count; index++)
            {
                double tick = this.slider.Ticks[index];
                double difference = System.Math.Abs(value - tick);
                if (difference < minimumDifference)
                {
                    minimumDifference = difference;
                    nearestTick = tick;
                }
            }

            return nearestTick;
        }
    }
}
