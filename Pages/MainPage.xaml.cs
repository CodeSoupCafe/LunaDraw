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
      CheckHideFlyouts();
    });
  }

  private void OnCanvasLoaded(object? sender, EventArgs e)
  {
    _viewModel.CanvasSize = new SKRect(0, 0, canvasView.CanvasSize.Width, canvasView.CanvasSize.Height);
    _viewModel.SaveState(); // Save the initial blank state
  }

  private void OnCanvasViewPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
  {
    SKSurface surface = e.Surface;
    SKCanvas canvas = surface.Canvas;

    canvas.Clear(SKColors.White);

    if (_viewModel == null) return;

    // Apply Navigation Transformation (Zoom/Pan)
    canvas.Concat(_viewModel.NavigationModel.TotalMatrix);

    // If we are in a snapshot state (after undo/redo), draw the picture
    if (_viewModel.CurrentSnapshot != null)
    {
      canvas.DrawPicture(_viewModel.CurrentSnapshot);
    }
    else // Otherwise, draw the live elements from the layers
    {
      foreach (var layer in _viewModel.Layers)
      {
        if (layer.IsVisible)
        {
          // Save layer to support blend modes (like Clear) working within the layer context
          // This ensures erasing works as expected (making things transparent relative to the layer start)
          // However, for simple erasing to white background, just drawing is often enough if the clear mode punches through.
          // But to be safe for potential transparency features, we use SaveLayer.
          // Actually, standard SaveLayer might be expensive.
          // Let's use it for now as requested for "ClearPath".
          var paint = new SKPaint();
          canvas.SaveLayer(paint);

          foreach (var element in layer.Elements)
          {
            if (element.IsVisible)
            {
              element.Draw(canvas);
            }
          }
          
          canvas.Restore();
        }
      }

      _viewModel.ActiveTool.DrawPreview(canvas, _viewModel);
    }
  }

  private void OnTouch(object? sender, SKTouchEventArgs e)
  {
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
    }
  }
}
