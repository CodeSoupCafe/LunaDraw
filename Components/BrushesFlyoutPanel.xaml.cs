using LunaDraw.Logic.Messages;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Components
{
  public partial class BrushesFlyoutPanel : ContentView
  {
    public BrushesFlyoutPanel()
    {
      InitializeComponent();
      this.Loaded += BrushesFlyoutPanel_Loaded;
    }

    private void BrushesFlyoutPanel_Loaded(object? sender, EventArgs e)
    {
      if (BindingContext is ToolbarViewModel toolbarViewModel)
      {
        GlowSwitch.IsToggled = toolbarViewModel.IsGlowEnabled;
        GlowRadiusSlider.Value = toolbarViewModel.GlowRadius;
        // Note: Glow Color selection isn't visually indicated in this simple UI yet, 
        // but we could update the selected border if we wanted to tracking state.
      }
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
  }
}