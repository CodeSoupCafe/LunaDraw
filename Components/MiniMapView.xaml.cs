using System.Reactive.Linq;

using LunaDraw.Logic.Messages;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LunaDraw.Components
{
  public partial class MiniMapView : ContentView
  {
    private MainViewModel? _viewModel;
    private SKMatrix _fitMatrix;

    public MiniMapView()
    {
      InitializeComponent();

      // Redraw when canvas is invalidated (drawing changes)
      MessageBus.Current.Listen<CanvasInvalidateMessage>()
          .Throttle(TimeSpan.FromMilliseconds(30), RxApp.MainThreadScheduler) // Throttle to avoid excessive redraws
          .Subscribe(_ => miniMapCanvas?.InvalidateSurface());
    }

    protected override void OnBindingContextChanged()
    {
      base.OnBindingContextChanged();
      _viewModel = BindingContext as MainViewModel;
      miniMapCanvas?.InvalidateSurface();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
      if (_viewModel == null) return;

      var canvas = e.Surface.Canvas;
      var info = e.Info;

      canvas.Clear(SKColors.White);

      // 1. Calculate Bounds of all elements
      var contentBounds = SKRect.Empty;
      bool hasContent = false;

      foreach (var layer in _viewModel.Layers)
      {
        if (!layer.IsVisible) continue;
        foreach (var element in layer.Elements)
        {
          if (!element.IsVisible) continue;
          var b = element.Bounds;
          if (hasContent)
            contentBounds.Union(b);
          else
          {
            contentBounds = b;
            hasContent = true;
          }
        }
      }

      if (!hasContent)
      {
        // Default to standard size if empty
        contentBounds = new SKRect(0, 0, 1000, 1000);
      }

      // Add some padding to bounds
      contentBounds.Inflate(50, 50);

      // 2. Calculate Fit Matrix (World -> MiniMap)
      // Scale to fit contentBounds into info.Rect
      float scaleX = info.Width / contentBounds.Width;
      float scaleY = info.Height / contentBounds.Height;
      float scale = Math.Min(scaleX, scaleY);

      // Center it
      float tx = (info.Width - contentBounds.Width * scale) / 2 - contentBounds.Left * scale;
      float ty = (info.Height - contentBounds.Height * scale) / 2 - contentBounds.Top * scale;

      _fitMatrix = SKMatrix.CreateScale(scale, scale);
      _fitMatrix = SKMatrix.Concat(SKMatrix.CreateTranslation(tx, ty), _fitMatrix);

      // 3. Draw Content
      canvas.Save();
      canvas.Concat(_fitMatrix);

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
      canvas.Restore();

      // 4. Draw Viewport Indicator
      // Viewport is the inverse of the Main Canvas Matrix applied to the Main Canvas Screen Rect
      if (_viewModel.NavigationModel.TotalMatrix.TryInvert(out var mainInverse))
      {
        // Main Canvas Screen Size (Approximate if not bound, but we can use the ViewModel's stored size)
        var mainScreenRect = _viewModel.CanvasSize;
        if (mainScreenRect.Width > 0)
        {
          // Map Screen Rect Corners -> World Points
          var tl = mainInverse.MapPoint(new SKPoint(mainScreenRect.Left, mainScreenRect.Top));
          var tr = mainInverse.MapPoint(new SKPoint(mainScreenRect.Right, mainScreenRect.Top));
          var br = mainInverse.MapPoint(new SKPoint(mainScreenRect.Right, mainScreenRect.Bottom));
          var bl = mainInverse.MapPoint(new SKPoint(mainScreenRect.Left, mainScreenRect.Bottom));

          // Map World Points -> MiniMap Points
          var m_tl = _fitMatrix.MapPoint(tl);
          var m_tr = _fitMatrix.MapPoint(tr);
          var m_br = _fitMatrix.MapPoint(br);
          var m_bl = _fitMatrix.MapPoint(bl);

          using var path = new SKPath();
          path.MoveTo(m_tl);
          path.LineTo(m_tr);
          path.LineTo(m_br);
          path.LineTo(m_bl);
          path.Close();

          using var paint = new SKPaint
          {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Red,
            StrokeWidth = 2,
            IsAntialias = true
          };
          canvas.DrawPath(path, paint);

          using var fillPaint = new SKPaint
          {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Red.WithAlpha(50)
          };
          canvas.DrawPath(path, fillPaint);
        }
      }
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
      if (_viewModel == null) return;

      // Only process if the user is actually touching/clicking (avoids hover issues)
      if (!e.InContact)
        return;

      var canvasView = sender as SKCanvasView;
      if (canvasView == null) return;

      switch (e.ActionType)
      {
        case SKTouchAction.Pressed:
        case SKTouchAction.Moved:
          // Convert DIPs (Touch Location) to Pixels (Canvas Coordinates)
          // _fitMatrix is calculated based on Pixels (e.Info in OnPaintSurface)
          double scaleFactor = 1.0;
          if (canvasView.Width > 0)
          {
            scaleFactor = canvasView.CanvasSize.Width / canvasView.Width;
          }

          var touchPixels = new SKPoint((float)(e.Location.X * scaleFactor), (float)(e.Location.Y * scaleFactor));

          // Move main view to this location
          if (_fitMatrix.TryInvert(out var inverseFit))
          {
            var worldPoint = inverseFit.MapPoint(touchPixels);

            // We want to center the Main View on this worldPoint.
            // Main View Matrix: Scale * Translation.
            // We want to keep current Scale.

            float currentScaleX = _viewModel.NavigationModel.TotalMatrix.ScaleX;
            float currentScaleY = _viewModel.NavigationModel.TotalMatrix.ScaleY;

            // If rotated, extracting scale is harder. 
            // Let's assume we want to Pan the view so that worldPoint is at Center of Screen.

            // Target: TotalMatrix.MapPoint(worldPoint) = ScreenCenter
            // TotalMatrix = [Existing Rotation/Scale] * [New Translation] ? 
            // No, we just want to adjust Translation.

            // Let's calculate the required translation.
            // ViewPoint = Matrix * WorldPoint
            // We want ViewPoint = ScreenCenter.

            // If we reconstruct the matrix:
            // NewMatrix = OldMatrix without Translation * NewTranslation? 
            // This clears rotation if we aren't careful.

            // Better approach: Calculate the difference.
            var currentViewPoint = _viewModel.NavigationModel.TotalMatrix.MapPoint(worldPoint);
            var screenCenter = new SKPoint(_viewModel.CanvasSize.Width / 2, _viewModel.CanvasSize.Height / 2);

            var delta = screenCenter - currentViewPoint;

            // Translate the view by delta
            var translation = SKMatrix.CreateTranslation(delta.X, delta.Y);
            _viewModel.NavigationModel.TotalMatrix = SKMatrix.Concat(translation, _viewModel.NavigationModel.TotalMatrix);

            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
          }
          e.Handled = true;
          break;
      }
    }
  }
}
