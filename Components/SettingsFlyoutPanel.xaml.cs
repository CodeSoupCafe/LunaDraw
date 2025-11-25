using Microsoft.Maui.Controls;
using SkiaSharp;

namespace LunaDraw.Components
{
    public partial class SettingsFlyoutPanel : ContentView
    {
        public static readonly BindableProperty StrokeColorProperty =
            BindableProperty.Create(
                nameof(StrokeColor),
                typeof(SKColor),
                typeof(SettingsFlyoutPanel),
                SKColors.Black);

        public static readonly BindableProperty FillColorProperty =
            BindableProperty.Create(
                nameof(FillColor),
                typeof(SKColor?),
                typeof(SettingsFlyoutPanel),
                (SKColor?)null);

        public static readonly BindableProperty TransparencyProperty =
            BindableProperty.Create(
                nameof(Transparency),
                typeof(float),
                typeof(SettingsFlyoutPanel),
                1.0f);

        public SettingsFlyoutPanel()
        {
            InitializeComponent();
        }

        public SKColor StrokeColor
        {
            get => (SKColor)GetValue(StrokeColorProperty);
            set => SetValue(StrokeColorProperty, value);
        }

        public SKColor? FillColor
        {
            get => (SKColor?)GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }

        public float Transparency
        {
            get => (float)GetValue(TransparencyProperty);
            set => SetValue(TransparencyProperty, value);
        }

    }
}
