// <copyright file="EncodingQualitySliderControl.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Controls
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for EncodingQualitySliderControl.
    /// </summary>
    public partial class EncodingQualitySliderControl : UserControl
    {
        public static readonly DependencyProperty BitrateProperty = DependencyProperty.Register(
            "Bitrate",
            typeof(double),
            typeof(EncodingQualitySliderControl),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(EncodingQualitySliderControl.OnBitrateValueChanged), new CoerceValueCallback(EncodingQualitySliderControl.CoerceBitrateValue)));

        public static readonly DependencyProperty EncodingModeProperty = DependencyProperty.Register(
            "EncodingMode",
            typeof(EncodingMode),
            typeof(EncodingQualitySliderControl),
            new PropertyMetadata(new PropertyChangedCallback(EncodingQualitySliderControl.OnEncodingModeValueChanged)));

        public EncodingQualitySliderControl()
        {
            this.InitializeComponent();

            this.CoerceValue(EncodingQualitySliderControl.BitrateProperty);

            this.slider.ValueChanged += this.Slider_ValueChanged;
        }
        
        public EncodingMode EncodingMode
        {
            get
            {
                return (EncodingMode)this.GetValue(EncodingQualitySliderControl.EncodingModeProperty);
            }

            set
            {
                this.SetCurrentValue(EncodingQualitySliderControl.EncodingModeProperty, value);
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
                this.SetCurrentValue(EncodingQualitySliderControl.BitrateProperty, this.GetNearestTickValue(value));
            }
        }
        
        private static void OnBitrateValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            EncodingQualitySliderControl encodingQualitySliderControl = sender as EncodingQualitySliderControl;
            encodingQualitySliderControl.slider.Value = (double)eventArgs.NewValue;
        }

        private static object CoerceBitrateValue(DependencyObject sender, object basevalue)
        {
            EncodingQualitySliderControl encodingQualitySliderControl = sender as EncodingQualitySliderControl;
            return encodingQualitySliderControl.GetNearestTickValue((double)basevalue);
        }

        private static void OnEncodingModeValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            EncodingQualitySliderControl encodingQualitySliderControl = sender as EncodingQualitySliderControl;
            Slider sliderControl = encodingQualitySliderControl.slider;
            
            switch (encodingQualitySliderControl.EncodingMode)
            {
                case EncodingMode.Mp3VBR:
                    {
                        sliderControl.Minimum = 65;
                        sliderControl.Maximum = 245;
                        sliderControl.SelectionStart = 115;
                        sliderControl.SelectionEnd = 245;
                        sliderControl.Ticks.Clear();
                        sliderControl.Ticks.Add(65);
                        sliderControl.Ticks.Add(85);
                        sliderControl.Ticks.Add(100);
                        sliderControl.Ticks.Add(115);
                        sliderControl.Ticks.Add(130);
                        sliderControl.Ticks.Add(165);
                        sliderControl.Ticks.Add(175);
                        sliderControl.Ticks.Add(190);
                        sliderControl.Ticks.Add(225);
                        sliderControl.Ticks.Add(245);
                        break;
                    }

                case EncodingMode.Mp3CBR:
                    {
                        sliderControl.Minimum = 8;
                        sliderControl.Maximum = 320;
                        sliderControl.SelectionStart = 128;
                        sliderControl.SelectionEnd = 256;
                        sliderControl.Ticks.Clear();
                        sliderControl.Ticks.Add(8);
                        sliderControl.Ticks.Add(16);
                        sliderControl.Ticks.Add(24);
                        sliderControl.Ticks.Add(32);
                        sliderControl.Ticks.Add(40);
                        sliderControl.Ticks.Add(48);
                        sliderControl.Ticks.Add(64);
                        sliderControl.Ticks.Add(80);
                        sliderControl.Ticks.Add(96);
                        sliderControl.Ticks.Add(112);
                        sliderControl.Ticks.Add(128);
                        sliderControl.Ticks.Add(160);
                        sliderControl.Ticks.Add(192);
                        sliderControl.Ticks.Add(224);
                        sliderControl.Ticks.Add(256);
                        sliderControl.Ticks.Add(320);
                        break;
                    }

                case EncodingMode.OggVBR:
                    {
                        sliderControl.Minimum = 32;
                        sliderControl.Maximum = 500;
                        sliderControl.SelectionStart = 80;
                        sliderControl.SelectionEnd = 192;
                        sliderControl.Ticks.Clear();
                        sliderControl.Ticks.Add(32);
                        sliderControl.Ticks.Add(48);
                        sliderControl.Ticks.Add(64);
                        sliderControl.Ticks.Add(80);
                        sliderControl.Ticks.Add(96);
                        sliderControl.Ticks.Add(112);
                        sliderControl.Ticks.Add(128);
                        sliderControl.Ticks.Add(160);
                        sliderControl.Ticks.Add(192);
                        sliderControl.Ticks.Add(224);
                        sliderControl.Ticks.Add(256);
                        sliderControl.Ticks.Add(320);
                        sliderControl.Ticks.Add(500);
                        break;
                    }

                case EncodingMode.AacVBR:
                    {
                        sliderControl.Minimum = 52;
                        sliderControl.Maximum = 208;
                        sliderControl.SelectionStart = 72;
                        sliderControl.SelectionEnd = 136;
                        sliderControl.Ticks.Clear();
                        sliderControl.Ticks.Add(52);
                        sliderControl.Ticks.Add(72);
                        sliderControl.Ticks.Add(104);
                        sliderControl.Ticks.Add(136);
                        sliderControl.Ticks.Add(208);
                        break;
                    }
            }

            encodingQualitySliderControl.CoerceValue(EncodingQualitySliderControl.BitrateProperty);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Bitrate == e.NewValue)
            {
                return;
            }

            this.SetCurrentValue(EncodingQualitySliderControl.BitrateProperty, e.NewValue);
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
