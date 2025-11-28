using LunaDraw.Logic.Messages;
using LunaDraw.Logic.ViewModels;
using LunaDraw.Logic.Extensions;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Pages;

public partial class MainPage : ContentPage
{
  private readonly MainViewModel _viewModel;
  private readonly ToolbarViewModel _toolbarViewModel;

  public MainPage(MainViewModel viewModel, ToolbarViewModel toolbarViewModel)
  {
    InitializeComponent();
    _viewModel = viewModel;
    _toolbarViewModel = toolbarViewModel;

    BindingContext = _viewModel;
    toolbarView.BindingContext = _toolbarViewModel;

    // Set up flyout content binding contexts
    SettingsFlyoutContent.BindingContext = _toolbarViewModel;
    ShapesFlyoutContent.BindingContext = _toolbarViewModel;
    BrushesFlyoutContent.BindingContext = _toolbarViewModel;

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
    // Use logical size
    _viewModel.CanvasSize = new SKRect(0, 0, (float)canvasView.Width, (float)canvasView.Height);
  }

  private void OnCanvasViewPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
  {
    SKSurface surface = e.Surface;
    SKCanvas canvas = surface.Canvas;

    // Ensure ViewModel knows the current canvas size (logical pixels)
    if (_viewModel != null)
    {
        // Use logical size
        _viewModel.CanvasSize = new SKRect(0, 0, (float)canvasView.Width, (float)canvasView.Height);
    }

    canvas.Clear(SKColors.White);

    if (_viewModel == null) return;

    canvas.Save();

    // Apply Navigation Transformation (User Transforms)
    canvas.Concat(_viewModel.NavigationModel.UserMatrix);

    // Apply Fit-To-Screen logic and capture Total Matrix (Legacy Pipeline)
    // We use the current surface size as the bounds to center, effectively maintaining a 1:1 aspect initially
    // but allowing the legacy MaxScaleCentered logic to calculate the final TotalMatrix.
    var bounds = new SKRect(0, 0, e.Info.Width, e.Info.Height);
    _viewModel.NavigationModel.TotalMatrix = canvas.MaxScaleCentered(e.Info.Width, e.Info.Height, bounds);

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
    
    canvas.Restore();
  }

  private void OnTouch(object? sender, SKTouchEventArgs e)
  {
    if (e.ActionType == SKTouchAction.Pressed)
    {
         CheckHideFlyouts();
    }
    _viewModel?.ProcessTouch(e, canvasView);
    e.Handled = true;
  }

  private void OnCanvasTapped(object? sender, TappedEventArgs e)
  {
    CheckHideFlyouts();
  }

  private void CheckHideFlyouts()
  {
    if (_toolbarViewModel.IsAnyFlyoutOpen)
    {
      _toolbarViewModel.IsSettingsOpen = false;
      _toolbarViewModel.IsShapesFlyoutOpen = false;
      _toolbarViewModel.IsBrushesFlyoutOpen = false;
    }
  }
}
