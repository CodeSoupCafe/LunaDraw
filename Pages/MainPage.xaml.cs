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

    canvasView.PaintSurface += OnCanvasViewPaintSurface;
    canvasView.Touch += OnTouch;

    MessageBus.Current.Listen<CanvasInvalidateMessage>().Subscribe(_ =>
    {
      canvasView?.InvalidateSurface();
    });
  }

  private void OnCanvasViewPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
  {
    SKSurface surface = e.Surface;
    SKCanvas canvas = surface.Canvas;

    // Ensure ViewModel knows the current canvas size (logical pixels)
    int width = e.BackendRenderTarget.Width;
    int height = e.BackendRenderTarget.Height;
    _viewModel.CanvasSize = new SKRect(0, 0, width, height);

    canvas.Clear(SKColors.White);

    if (_viewModel == null) return;

    canvas.Save();

    lock (_viewModel.NavigationModel.MatrixLock)
    {
        // Apply Navigation Transformation (User Transforms)
        canvas.Concat(_viewModel.NavigationModel.UserMatrix);

        // Apply Fit-To-Screen logic and capture Total Matrix (Legacy Pipeline)
        var bounds = new SKRect(0, 0, width, height);
        _viewModel.NavigationModel.TotalMatrix = canvas.MaxScaleCentered(width, height, bounds);
    }

    foreach (var layer in _viewModel.Layers)
    {
      if (layer.IsVisible)
      {
        layer.Draw(canvas);
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
    _viewModel?.ProcessTouch(e);
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
