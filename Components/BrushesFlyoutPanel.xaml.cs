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
      this.Loaded += OnBrushesFlyoutPanelLoaded;
    }

    private void OnBrushesFlyoutPanelLoaded(object? sender, EventArgs e)
    {
      if (BindingContext is ToolbarViewModel toolbarViewModel)
      {
          // No settings to load here anymore, just brush shapes which are data bound
      }
    }
  }
}
