namespace LunaDraw.Components.Carousel;

using LunaDraw.Logic.Models;
using SkiaSharp;
using SkiaSharp.Views.Maui;

public partial class RenderCanvas : ContentView, IDisposable
{
  public static readonly BindableProperty DrawingProperty =
     BindableProperty.Create(nameof(Drawing), typeof(External.Drawing), typeof(RenderCanvas), propertyChanged: OnDrawingChanged);

  private readonly List<IDisposable> subscriptions = new List<IDisposable>();
  private bool isDisposed;

  public RenderCanvas()
  {
    InitializeComponent();

    if (CanvasViewRef != null)
    {
      CanvasViewRef.PaintSurface += OnCanvasViewPaintSurface;
    }

    // Subscribe to GlobalBroadcaster for scroll-based rendering optimization
    subscriptions.Add(GlobalBroadcaster.Subscribe<ImageLoadingState>(this,
      AppMessageStateType.ImageLoadingState,
      (_, imageLoadingState) =>
      {
        if (imageLoadingState.LoadingState == ImageLoadingType.ForceRedraw)
        {
          CanvasViewRef?.InvalidateSurface();
        }
      }));
  }

  public External.Drawing? Drawing
  {
    get => (External.Drawing?)GetValue(DrawingProperty);
    set => SetValue(DrawingProperty, value);
  }

  private static void OnDrawingChanged(BindableObject bindable, object oldValue, object newValue)
  {
    if (bindable is RenderCanvas canvas && !canvas.isDisposed)
    {
      canvas.CanvasViewRef?.InvalidateSurface();
    }
  }


  public void OnCanvasViewPaintSurface(object? sender, SKPaintSurfaceEventArgs args)
  {
    if (isDisposed || args?.Surface?.Canvas == null)
      return;

    try
    {
      SKCanvas canvas = args.Surface.Canvas;
      canvas.Clear(SKColors.White);

      if (Drawing == null)
      {
        using var paint = new SKPaint { Color = SKColors.LightGray, IsAntialias = true };
        canvas.DrawRect(args.Info.Rect, paint);
        return;
      }

      var drawingWidth = Drawing.CanvasWidth > 0 ? Drawing.CanvasWidth : 800;
      var drawingHeight = Drawing.CanvasHeight > 0 ? Drawing.CanvasHeight : 600;

      var scaleX = args.Info.Width / (float)drawingWidth;
      var scaleY = args.Info.Height / (float)drawingHeight;
      var scale = Math.Min(scaleX, scaleY);

      canvas.Save();

      var scaledWidth = drawingWidth * scale;
      var scaledHeight = drawingHeight * scale;
      var dx = (args.Info.Width - scaledWidth) / 2f;
      var dy = (args.Info.Height - scaledHeight) / 2f;

      canvas.Translate(dx, dy);
      canvas.Scale(scale);

      try
      {
        if (Drawing.Layers == null || Drawing.Layers.Count == 0)
        {
          System.Diagnostics.Debug.WriteLine($"[RenderCanvas] Drawing {Drawing.Name} has no layers");
          canvas.Restore();
          return;
        }

        var layers = LunaDraw.Logic.Utils.DrawingStorageMomento.RestoreLayersStatic(Drawing);

        if (layers == null || layers.Count == 0)
        {
          System.Diagnostics.Debug.WriteLine($"[RenderCanvas] No layers restored for drawing {Drawing.Name}");
          canvas.Restore();
          return;
        }

        foreach (var layer in layers)
        {
          if (layer != null && layer.IsVisible)
          {
            layer.Draw(canvas);
          }
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"[RenderCanvas] Error restoring/drawing layers for {Drawing?.Name ?? "unknown"}: {ex}");
        System.Diagnostics.Debug.WriteLine($"[RenderCanvas] Stack trace: {ex.StackTrace}");
      }

      canvas.Restore();
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"[RenderCanvas] Error rendering: {ex}");
      System.Diagnostics.Debug.WriteLine($"[RenderCanvas] Stack trace: {ex.StackTrace}");
    }
  }

  public void Dispose()
  {
    if (isDisposed)
      return;

    isDisposed = true;

    try
    {
      subscriptions?.ForEach(x => x.Dispose());
      subscriptions?.Clear();

      if (CanvasViewRef != null)
      {
        CanvasViewRef.PaintSurface -= OnCanvasViewPaintSurface;
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"[RenderCanvas] Error during disposal: {ex}");
    }

    GC.SuppressFinalize(this);
  }
}