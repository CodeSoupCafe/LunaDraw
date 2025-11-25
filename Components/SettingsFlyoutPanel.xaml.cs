using LunaDraw.Logic.Messages;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

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
            SKColors.Black,
            propertyChanged: OnStrokeColorPropertyChanged);

    public static readonly BindableProperty FillColorProperty =
        BindableProperty.Create(
            nameof(FillColor),
            typeof(SKColor?),
            typeof(SettingsFlyoutPanel),
            (SKColor?)null,
            propertyChanged: OnFillColorPropertyChanged);

    public static readonly BindableProperty TransparencyProperty =
        BindableProperty.Create(
            nameof(Transparency),
            typeof(byte),
            typeof(SettingsFlyoutPanel),
            (byte)255,
            propertyChanged: OnTransparencyPropertyChanged);

    public SettingsFlyoutPanel()
    {
      InitializeComponent();
      this.Loaded += SettingsFlyoutPanel_Loaded;
    }

    private void SettingsFlyoutPanel_Loaded(object sender, EventArgs e)
    {
      if (BindingContext is ToolbarViewModel toolbarViewModel)
      {
        // Set initial values from ViewModel
        StrokeColorPicker.PickedColor = SKColorToMauiColor(toolbarViewModel.StrokeColor);
        if (toolbarViewModel.FillColor.HasValue)
          FillColorPicker.PickedColor = SKColorToMauiColor(toolbarViewModel.FillColor.Value);
        TransparencySlider.Value = toolbarViewModel.Opacity;
      }
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

    public byte Transparency
    {
      get => (byte)GetValue(TransparencyProperty);
      set => SetValue(TransparencyProperty, value);
    }

    private static void OnStrokeColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
      var panel = (SettingsFlyoutPanel)bindable;
      if (panel.BindingContext is ToolbarViewModel toolbarViewModel && newValue is SKColor color)
      {
        toolbarViewModel.StrokeColor = color;
        panel.StrokeColorPicker.PickedColor = SKColorToMauiColor(color);
      }
    }

    private static void OnFillColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
      var panel = (SettingsFlyoutPanel)bindable;
      if (panel.BindingContext is ToolbarViewModel toolbarViewModel)
      {
        toolbarViewModel.FillColor = newValue as SKColor?;
        if (newValue is SKColor fillColor)
          panel.FillColorPicker.PickedColor = SKColorToMauiColor(fillColor);
      }
    }

    private static void OnTransparencyPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
      var panel = (SettingsFlyoutPanel)bindable;
      if (panel.BindingContext is ToolbarViewModel toolbarViewModel && newValue is byte transparency)
      {
        toolbarViewModel.Opacity = transparency;
        panel.TransparencySlider.Value = transparency;
      }
    }

    private void OnStrokeColorChanged(object sender, EventArgs e)
    {
      if (sender is Maui.ColorPicker.ColorPicker colorPicker)
      {
        var strokeColor = MauiColorToSKColor(colorPicker.PickedColor);
        MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(strokeColor: strokeColor));
      }
    }

    private void OnFillColorChanged(object sender, EventArgs e)
    {
      if (sender is Maui.ColorPicker.ColorPicker colorPicker)
      {
        var fillColor = MauiColorToSKColor(colorPicker.PickedColor);
        MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(fillColor: fillColor));
      }
    }

    private void OnTransparencyChanged(object sender, ValueChangedEventArgs e)
    {
      var transparency = (byte)e.NewValue;
      MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(transparency: transparency));
    }

    private static Color SKColorToMauiColor(SKColor skColor)
    {
      return Color.FromRgba(skColor.Red, skColor.Green, skColor.Blue, skColor.Alpha);
    }

    private static SKColor MauiColorToSKColor(Color mauiColor)
    {
      return new SKColor(
          (byte)((mauiColor?.Red ?? 0) * 255),
          (byte)((mauiColor?.Green ?? 0) * 255),
          (byte)((mauiColor?.Blue ?? 0) * 255),
          (byte)((mauiColor?.Alpha ?? 0) * 255));
    }
  }
}
