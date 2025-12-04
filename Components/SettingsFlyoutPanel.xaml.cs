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

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(
            nameof(Size),
            typeof(float),
            typeof(SettingsFlyoutPanel),
            5.0f,
            propertyChanged: OnSizePropertyChanged);

    public static readonly BindableProperty FlowProperty =
        BindableProperty.Create(
            nameof(Flow),
            typeof(byte),
            typeof(SettingsFlyoutPanel),
            (byte)255,
            propertyChanged: OnFlowPropertyChanged);

    public static readonly BindableProperty SpacingProperty =
        BindableProperty.Create(
            nameof(Spacing),
            typeof(float),
            typeof(SettingsFlyoutPanel),
            0.25f,
            propertyChanged: OnSpacingPropertyChanged);

    private bool suppressEvents;

    public SettingsFlyoutPanel()
    {
      InitializeComponent();
      this.Loaded += OnSettingsFlyoutPanelLoaded;
    }

    private void OnSettingsFlyoutPanelLoaded(object? sender, EventArgs e)
    {
      if (BindingContext is ToolbarViewModel toolbarViewModel)
      {
        // Set initial values from ViewModel
        StrokeColorPicker.PickedColor = SKColorToMauiColor(toolbarViewModel.StrokeColor);

        if (toolbarViewModel.FillColor.HasValue)
          FillColorPicker.PickedColor = SKColorToMauiColor(toolbarViewModel.FillColor.Value);

        TransparencySlider.Value = toolbarViewModel.Opacity;
        SizeSlider.Value = toolbarViewModel.StrokeWidth;
        FlowSlider.Value = toolbarViewModel.Flow;
        SpacingSlider.Value = toolbarViewModel.Spacing;

        GlowSwitch.IsToggled = toolbarViewModel.IsGlowEnabled;
        GlowRadiusSlider.Value = toolbarViewModel.GlowRadius;
        RainbowSwitch.IsToggled = toolbarViewModel.IsRainbowEnabled;
        ScatterSlider.Value = toolbarViewModel.ScatterRadius;
        SizeJitterSlider.Value = toolbarViewModel.SizeJitter;
        AngleJitterSlider.Value = toolbarViewModel.AngleJitter;
        HueJitterSlider.Value = toolbarViewModel.HueJitter;
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

    public float Size
    {
      get => (float)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public byte Flow
    {
      get => (byte)GetValue(FlowProperty);
      set => SetValue(FlowProperty, value);
    }

    public float Spacing
    {
      get => (float)GetValue(SpacingProperty);
      set => SetValue(SpacingProperty, value);
    }

    private static void OnStrokeColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        try
        {
            var panel = (SettingsFlyoutPanel)bindable;
            if (panel.BindingContext is ToolbarViewModel toolbarViewModel && newValue is SKColor color)
            {
                if (MessageBus.Current != null)
                    MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(strokeColor: color));
                if (panel.StrokeColorPicker != null)
                    panel.StrokeColorPicker.PickedColor = SKColorToMauiColor(color);
            }
        }
        catch { }
    }

    private static void OnFillColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        try
        {
            var panel = (SettingsFlyoutPanel)bindable;
            if (panel.BindingContext is ToolbarViewModel toolbarViewModel)
            {
                var fill = newValue as SKColor?;
                if (MessageBus.Current != null)
                    MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(fillColor: fill));
                if (newValue is SKColor fillColor && panel.FillColorPicker != null)
                    panel.FillColorPicker.PickedColor = SKColorToMauiColor(fillColor);
            }
        }
        catch { }
    }

    private static void OnTransparencyPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        try
        {
            var panel = (SettingsFlyoutPanel)bindable;
            if (panel.BindingContext is ToolbarViewModel toolbarViewModel && newValue is byte transparency)
            {
                if (MessageBus.Current != null)
                    MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(transparency: transparency));
                if (panel.TransparencySlider != null)
                    panel.TransparencySlider.Value = transparency;
            }
        }
        catch { }
    }

    private static void OnSizePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        try
        {
            var panel = (SettingsFlyoutPanel)bindable;
            if (panel.BindingContext is ToolbarViewModel && newValue is float size)
            {
                if (MessageBus.Current != null)
                    MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(strokeWidth: size));
                if (panel.SizeSlider != null)
                    panel.SizeSlider.Value = size;
            }
        }
        catch { }
    }

    private static void OnFlowPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        try
        {
            var panel = (SettingsFlyoutPanel)bindable;
            if (panel.BindingContext is ToolbarViewModel && newValue is byte flow)
            {
                if (MessageBus.Current != null)
                    MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(flow: flow));
                if (panel.FlowSlider != null)
                    panel.FlowSlider.Value = flow;
            }
        }
        catch { }
    }

    private static void OnSpacingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        try
        {
            var panel = (SettingsFlyoutPanel)bindable;
            if (panel.BindingContext is ToolbarViewModel && newValue is float spacing)
            {
                if (MessageBus.Current != null)
                    MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(spacing: spacing));
                if (panel.SpacingSlider != null)
                    panel.SpacingSlider.Value = spacing;
            }
        }
        catch { }
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
      if (suppressEvents) return;

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

    private void OnSizeChanged(object sender, ValueChangedEventArgs e)
    {
      var size = (float)e.NewValue;
      MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(strokeWidth: size));
    }

    private void OnFlowChanged(object sender, ValueChangedEventArgs e)
    {
      var flow = (byte)e.NewValue;
      MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(flow: flow));
    }

    private void OnSpacingChanged(object sender, ValueChangedEventArgs e)
    {
      var spacing = (float)e.NewValue;
      MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(spacing: spacing));
    }

    private void OnNoFillClicked(object sender, EventArgs e)
    {
      suppressEvents = true;
      try
      {
        FillColorPicker.PickedColor = Colors.Transparent; // Clear the color picker visually
      }
      finally
      {
        suppressEvents = false;
      }
      MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(shouldClearFillColor: true));
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

    private void OnGlowSwitchToggled(object sender, ToggledEventArgs e)
    {
      MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(isGlowEnabled: e.Value));
    }

    private void OnGlowRadiusChanged(object sender, ValueChangedEventArgs e)
    {
      MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(glowRadius: (float)e.NewValue));
    }

    private void OnGlowColorTapped(object sender, TappedEventArgs e)
    {
      if (e.Parameter is string hexColor)
      {
        if (SKColor.TryParse(hexColor, out var color))
        {
          MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(glowColor: color));
        }
      }
    }

    private void OnRainbowSwitchToggled(object sender, ToggledEventArgs e)
    {
        MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(isRainbowEnabled: e.Value));
    }

    private void OnScatterChanged(object sender, ValueChangedEventArgs e)
    {
        MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(scatterRadius: (float)e.NewValue));
    }

    private void OnSizeJitterChanged(object sender, ValueChangedEventArgs e)
    {
        MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(sizeJitter: (float)e.NewValue));
    }

    private void OnAngleJitterChanged(object sender, ValueChangedEventArgs e)
    {
        MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(angleJitter: (float)e.NewValue));
    }

    private void OnHueJitterChanged(object sender, ValueChangedEventArgs e)
    {
        MessageBus.Current.SendMessage(new BrushSettingsChangedMessage(hueJitter: (float)e.NewValue));
    }
  }
}
