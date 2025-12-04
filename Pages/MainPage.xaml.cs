using LunaDraw.Logic.Messages;
using LunaDraw.Logic.ViewModels;
using LunaDraw.Logic.Extensions;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Pages;

public partial class MainPage : ContentPage
{
  private readonly MainViewModel viewModel;
  private readonly ToolbarViewModel toolbarViewModel;

  public MainPage(MainViewModel viewModel, ToolbarViewModel toolbarViewModel)
  {
    InitializeComponent();
    this.viewModel = viewModel;
    this.toolbarViewModel = toolbarViewModel;

    BindingContext = this.viewModel;
    toolbarView.BindingContext = this.toolbarViewModel;

    // Set up flyout content binding contexts
    SettingsFlyoutContent.BindingContext = this.toolbarViewModel;
    ShapesFlyoutContent.BindingContext = this.toolbarViewModel;
    BrushesFlyoutContent.BindingContext = this.toolbarViewModel;

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
    viewModel.CanvasSize = new SKRect(0, 0, width, height);

    canvas.Clear(SKColors.White);

    if (viewModel == null) return;

    canvas.Save();

    // DIRECT FIX: Use SetMatrix directly with UserMatrix.
    // The UserMatrix is now the single source of truth for View Transformation (Pan/Zoom/Rotate).
    // We do not mix it with MaxScaleCentered or other legacy logic.
    canvas.SetMatrix(viewModel.NavigationModel.UserMatrix);
    
    // Sync TotalMatrix for Input Handler (reverse mapping)
    // Since we just SetMatrix, TotalMatrix IS UserMatrix.
    viewModel.NavigationModel.TotalMatrix = canvas.TotalMatrix;

    foreach (var layer in viewModel.Layers)
    {
      if (layer.IsVisible)
      {
        layer.Draw(canvas);
      }
    }

    viewModel.ActiveTool.DrawPreview(canvas, viewModel);
    
    canvas.Restore();
  }

  private void OnTouch(object? sender, SKTouchEventArgs e)
  {
    if (e.ActionType == SKTouchAction.Pressed)
    {
         CheckHideFlyouts();
    }
    viewModel?.ProcessTouch(e);
    e.Handled = true;
  }

  private void OnCanvasTapped(object? sender, TappedEventArgs e)
  {
    CheckHideFlyouts();
  }

  private void CheckHideFlyouts()
  {
    if (toolbarViewModel.IsAnyFlyoutOpen)
    {
      toolbarViewModel.IsSettingsOpen = false;
      toolbarViewModel.IsShapesFlyoutOpen = false;
      toolbarViewModel.IsBrushesFlyoutOpen = false;
    }
  }
}