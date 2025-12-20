using LunaDraw.Logic.Models;
using SkiaSharp;
using System.Collections.Concurrent;

namespace LunaDraw.Logic.Utils;

public class DrawingThumbnailFacade : IDrawingThumbnailFacade
{
  private readonly IDrawingStorageMomento drawingStorageMomento;
  private readonly ConcurrentDictionary<Guid, ImageSource> thumbnailCache = new();

  public DrawingThumbnailFacade(IDrawingStorageMomento drawingStorageMomento)
  {
    this.drawingStorageMomento = drawingStorageMomento;
  }

  public async Task<ImageSource?> GetThumbnailAsync(Guid drawingId, int width, int height)
  {
    // Check cache first
    if (thumbnailCache.TryGetValue(drawingId, out var cachedThumbnail))
    {
      return cachedThumbnail;
    }

    // Load drawing and generate thumbnail
    var drawing = await drawingStorageMomento.LoadDrawingAsync(drawingId);
    if (drawing == null)
    {
      return null;
    }

    var thumbnail = await GenerateThumbnailAsync(drawing, width, height);

    // Cache it
    if (thumbnail != null)
    {
      thumbnailCache[drawingId] = thumbnail;
    }

    return thumbnail;
  }

  public Task<ImageSource?> GenerateThumbnailAsync(Logic.Models.External.Drawing drawing, int width, int height)
  {
    try
    {
      // Create a bitmap to render to
      using var surface = SKSurface.Create(new SKImageInfo(width, height));
      var canvas = surface.Canvas;

      // Clear with transparent background
      canvas.Clear(SKColors.Transparent);

      // Calculate scale to fit drawing into thumbnail bounds
      var scaleX = (float)width / drawing.CanvasWidth;
      var scaleY = (float)height / drawing.CanvasHeight;
      var scale = Math.Min(scaleX, scaleY);

      // Center the drawing in the thumbnail
      var offsetX = (width - (drawing.CanvasWidth * scale)) / 2;
      var offsetY = (height - (drawing.CanvasHeight * scale)) / 2;

      canvas.Translate(offsetX, offsetY);
      canvas.Scale(scale);

      // Render all layers
      int elementCount = 0;
      foreach (var layer in drawing.Layers.Where(l => l.IsVisible))
      {
        foreach (var element in layer.Elements.Where(e => e.IsVisible).OrderBy(e => e.ZIndex))
        {
          RenderElement(canvas, element);
          elementCount++;
        }
      }

      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Rendered {elementCount} elements for drawing {drawing.Id}");

      // Create image from surface
      using var image = surface.Snapshot();
      using var data = image.Encode(SKEncodedImageFormat.Png, 100);

      if (data == null)
      {
        System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Failed to encode image for drawing {drawing.Id}");
        return Task.FromResult<ImageSource?>(null);
      }

      // Convert to byte array immediately while data is still valid
      var imageBytes = data.ToArray();

      if (imageBytes == null || imageBytes.Length == 0)
      {
        System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Empty byte array for drawing {drawing.Id}");
        return Task.FromResult<ImageSource?>(null);
      }

      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Generated thumbnail for {drawing.Id}: {imageBytes.Length} bytes");

      // Capture bytes in local variable for closure
      var bytesForStream = imageBytes;

      // Create ImageSource using the simpler FromStream pattern
      var imageSource = ImageSource.FromStream(() => new MemoryStream(bytesForStream));

      return Task.FromResult<ImageSource?>(imageSource);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Error generating thumbnail for {drawing.Id}: {ex.Message}");
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Stack trace: {ex.StackTrace}");
      return Task.FromResult<ImageSource?>(null);
    }
  }

  private void RenderElement(SKCanvas canvas, External.Element element)
  {
    System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] RenderElement called for type: {element.GetType().Name}");

    using var paint = new SKPaint
    {
      IsAntialias = true
    };

    // Don't apply per-element transform - stamps are already in canvas coordinates
    // The canvas is already scaled/translated at the drawing level

    if (element is External.Path pathElement)
    {
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Matched as Path");
      RenderPath(canvas, pathElement, paint);
    }
    else if (element is External.Stamps stampsElement)
    {
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Matched as Stamps");
      RenderStamps(canvas, stampsElement, paint);
    }
    else
    {
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Element type NOT matched - skipping render");
    }
  }

  private void RenderPath(SKCanvas canvas, External.Path pathElement, SKPaint paint)
  {
    if (string.IsNullOrEmpty(pathElement.PathData))
    {
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Path has no PathData");
      return;
    }

    using var path = SKPath.ParseSvgPathData(pathElement.PathData);
    if (path == null)
    {
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Failed to parse SVG path");
      return;
    }

    // Set stroke properties
    if (!string.IsNullOrEmpty(pathElement.StrokeColor))
    {
      SKColor.TryParse(pathElement.StrokeColor, out var strokeColor);
      paint.Color = strokeColor.WithAlpha(pathElement.Opacity);
      paint.StrokeWidth = pathElement.StrokeWidth;
      paint.Style = SKPaintStyle.Stroke;
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Drawing path stroke: color={pathElement.StrokeColor}, width={pathElement.StrokeWidth}, opacity={pathElement.Opacity}");
      canvas.DrawPath(path, paint);
    }

    // Set fill properties
    if (pathElement.IsFilled && !string.IsNullOrEmpty(pathElement.FillColor))
    {
      SKColor.TryParse(pathElement.FillColor, out var fillColor);
      paint.Color = fillColor.WithAlpha(pathElement.Opacity);
      paint.Style = SKPaintStyle.Fill;
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] Drawing path fill: color={pathElement.FillColor}, opacity={pathElement.Opacity}");
      canvas.DrawPath(path, paint);
    }
  }

  private void RenderStamps(SKCanvas canvas, External.Stamps stampsElement, SKPaint paint)
  {
    System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] RenderStamps - Points count: {stampsElement.Points?.Count ?? 0}");

    if (stampsElement.Points == null || !stampsElement.Points.Any())
    {
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] RenderStamps - No points, returning");
      return;
    }

    // Get brush shape path
    System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] RenderStamps - ShapeType: {stampsElement.ShapeType}");
    var shapePath = GetShapePath((BrushShapeType)stampsElement.ShapeType);
    if (shapePath == null)
    {
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] RenderStamps - shapePath is null, returning");
      return;
    }

    // Set paint properties
    System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] RenderStamps - StrokeColor: {stampsElement.StrokeColor}, Size: {stampsElement.Size}, IsFilled: {stampsElement.IsFilled}, Opacity: {stampsElement.Opacity}");

    if (!string.IsNullOrEmpty(stampsElement.StrokeColor))
    {
      SKColor.TryParse(stampsElement.StrokeColor, out var strokeColor);
      paint.Color = strokeColor.WithAlpha(stampsElement.Opacity);
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] RenderStamps - Paint color set to: {paint.Color}");
    }
    else
    {
      System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] RenderStamps - StrokeColor is empty!");
    }

    paint.Style = stampsElement.IsFilled ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
    paint.StrokeWidth = stampsElement.StrokeWidth;

    // Render each stamp point
    System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] RenderStamps - Starting to draw {stampsElement.Points.Count} stamps");
    for (int i = 0; i < stampsElement.Points.Count; i++)
    {
      var point = stampsElement.Points[i];
      var position = new SKPoint(point[0], point[1]);

      if (i == 0)
      {
        System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] RenderStamps - First stamp at position: {position}, Size: {stampsElement.Size}");
      }

      canvas.Save();
      canvas.Translate(position.X, position.Y);
      canvas.Scale(stampsElement.Size, stampsElement.Size);

      // Apply rotation if available
      if (stampsElement.Rotations != null && i < stampsElement.Rotations.Count)
      {
        canvas.RotateDegrees(stampsElement.Rotations[i]);
      }

      canvas.DrawPath(shapePath, paint);
      canvas.Restore();
    }
    System.Diagnostics.Debug.WriteLine($"[DrawingThumbnailFacade] RenderStamps - Finished drawing stamps");
  }

  private SKPath? GetShapePath(BrushShapeType shapeType)
  {
    // Get the brush shape - this is simplified, you might want to use BrushShape.GetPath()
    var path = new SKPath();

    switch (shapeType)
    {
      case BrushShapeType.Circle:
        path.AddCircle(0, 0, 0.5f);
        break;
      case BrushShapeType.Square:
        path.AddRect(new SKRect(-0.5f, -0.5f, 0.5f, 0.5f));
        break;
      case BrushShapeType.Star:
        // Simple 5-pointed star
        for (int i = 0; i < 5; i++)
        {
          var angle = (float)(i * 72 * Math.PI / 180);
          var outerX = (float)Math.Sin(angle) * 0.5f;
          var outerY = -(float)Math.Cos(angle) * 0.5f;

          if (i == 0)
            path.MoveTo(outerX, outerY);
          else
            path.LineTo(outerX, outerY);

          var innerAngle = (float)((i * 72 + 36) * Math.PI / 180);
          var innerX = (float)Math.Sin(innerAngle) * 0.2f;
          var innerY = -(float)Math.Cos(innerAngle) * 0.2f;
          path.LineTo(innerX, innerY);
        }
        path.Close();
        break;
      default:
        // Default to circle for other shapes
        path.AddCircle(0, 0, 0.5f);
        break;
    }

    return path;
  }

  public void InvalidateThumbnail(Guid drawingId)
  {
    thumbnailCache.TryRemove(drawingId, out _);
  }

  public void ClearCache()
  {
    thumbnailCache.Clear();
  }
}
