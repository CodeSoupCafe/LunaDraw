using LunaDraw.Logic.Messages;
using LunaDraw.Logic.ViewModels;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Extensions;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using ReactiveUI;

namespace LunaDraw.Pages;

public partial class PlaybackPage : ContentPage
{
  private readonly MainViewModel viewModel;
  private readonly IMessageBus messageBus;
  private readonly IPreferencesFacade preferencesFacade;
  private bool isCanvasReady = false;
  private bool isInvalidationPending = false;

  public PlaybackPage(
      MainViewModel viewModel,
      IMessageBus messageBus,
      IPreferencesFacade preferencesFacade)
  {
    InitializeComponent();
    this.viewModel = viewModel;
    this.messageBus = messageBus;
    this.preferencesFacade = preferencesFacade;

    this.messageBus.Listen<CanvasInvalidateMessage>().Subscribe(_ =>
    {
      SafeInvalidateSurface();
    });
  }

  private void SafeInvalidateSurface()
  {
    if (canvasView == null) return;

    if (!isCanvasReady)
    {
      isInvalidationPending = true;
      return;
    }

    MainThread.BeginInvokeOnMainThread(() =>
    {
      try
      {
        if (canvasView != null)
        {
          canvasView.InvalidateSurface();
        }
      }
      catch (Exception)
      {
         // Handle potential EGL context lost issues
      }
    });
  }

  private void OnCanvasViewPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
  {
    try
    {
      if (!isCanvasReady)
      {
        isCanvasReady = true;
        if (isInvalidationPending)
        {
          isInvalidationPending = false;
          Task.Delay(50).ContinueWith(_ => SafeInvalidateSurface());
        }
      }

      SKSurface surface = e.Surface;
      SKCanvas canvas = surface.Canvas;

      int width = e.BackendRenderTarget.Width;
      int height = e.BackendRenderTarget.Height;

      if (viewModel is null) return;

      var bgColor = preferencesFacade.GetCanvasBackgroundColor();
      canvas.Clear(bgColor);

      canvas.Save();

      // We use the same view matrix as the main drawing to ensure consistent view
      canvas.SetMatrix(viewModel.NavigationModel.ViewMatrix);

      // Draw layers
      var layers = viewModel.Layers;
      for (int i = 0; i < layers.Count; i++)
      {
        var layer = layers[i];
        if (!layer.IsVisible) continue;

        if (layer.MaskingMode == Logic.Models.MaskingMode.Clip)
        {
          layer.Draw(canvas);
        }
        else
        {
          bool hasClippingLayers = false;
          int nextIndex = i + 1;
          while (nextIndex < layers.Count && layers[nextIndex].MaskingMode == Logic.Models.MaskingMode.Clip)
          {
            if (layers[nextIndex].IsVisible) hasClippingLayers = true;
            nextIndex++;
          }

          if (hasClippingLayers)
          {
            canvas.SaveLayer();
            layer.Draw(canvas);

            using (var paint = new SKPaint { BlendMode = SKBlendMode.SrcATop, IsAntialias = true })
            {
              for (int j = i + 1; j < layers.Count; j++)
              {
                var clipLayer = layers[j];
                if (clipLayer.MaskingMode != Logic.Models.MaskingMode.Clip) break;

                if (clipLayer.IsVisible)
                {
                  canvas.SaveLayer(paint);
                  clipLayer.Draw(canvas);
                  canvas.Restore();
                }
                i = j;
              }
            }
            canvas.Restore();
          }
          else
          {
            layer.Draw(canvas);
          }
        }
      }

      canvas.Restore();
    }
    catch (Exception)
    {
    }
  }
}
