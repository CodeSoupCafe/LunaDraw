using LunaDraw.Logic.Messages;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Pages;

public partial class MainPage : ContentPage
{
  private MainViewModel _viewModel;
  private ToolbarViewModel _toolbarViewModel;

  public MainPage()
  {
    InitializeComponent();
    _viewModel = new MainViewModel();
    _toolbarViewModel = new ToolbarViewModel(_viewModel);
    BindingContext = _viewModel;
    toolbarView.BindingContext = _toolbarViewModel;

    // Set up flyout content binding contexts
    SettingsFlyoutContent.BindingContext = _toolbarViewModel;
    ShapesFlyoutContent.BindingContext = _toolbarViewModel;

    canvasView.Loaded += OnCanvasLoaded;
    canvasView.PaintSurface += OnCanvasViewPaintSurface;
    canvasView.Touch += OnTouch;

    MessageBus.Current.Listen<CanvasInvalidateMessage>().Subscribe(_ =>
    {
      canvasView?.InvalidateSurface();
    });
  }

  private void OnCanvasLoaded(object? sender, EventArgs e)
  {
    _viewModel.CanvasSize = new SKRect(0, 0, canvasView.CanvasSize.Width, canvasView.CanvasSize.Height);
  }

  private void OnCanvasViewPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
  {
    SKSurface surface = e.Surface;
    SKCanvas canvas = surface.Canvas;

    canvas.Clear(SKColors.White);

    if (_viewModel == null) return;

    // Apply Navigation Transformation (Zoom/Pan)
    canvas.Concat(_viewModel.NavigationModel.TotalMatrix);

    foreach (var layer in _viewModel.Layers)
    {
      if (layer.IsVisible)
      {
        foreach (var element in layer.Elements)
        {
          if (element.IsVisible)
          {
            element.Draw(canvas);
          }
        }
      }
    }

    _viewModel.ActiveTool.DrawPreview(canvas, _viewModel);
  }

  private void OnTouch(object? sender, SKTouchEventArgs e)
  {
    if (e.ActionType == SKTouchAction.Pressed)
    {
         CheckHideFlyouts();
    }
    _viewModel?.ProcessTouch(e);
    e.Handled = true;
  }

  private void OnCanvasTapped(object? sender, TappedEventArgs e)
  {
    // CheckHideFlyouts(); // Redundant if handled in OnTouch, but keeping for safety if Touch doesn't fire for Tap
    CheckHideFlyouts();
  }

  private void CheckHideFlyouts()
  {
    if (_toolbarViewModel.IsAnyFlyoutOpen)
    {
      _toolbarViewModel.IsSettingsOpen = false;
      _toolbarViewModel.IsShapesFlyoutOpen = false;
    }
  }
}
