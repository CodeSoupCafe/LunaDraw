/* 
 *  Copyright (c) 2025 CodeSoupCafe LLC
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 *  
 */

using LunaDraw.Logic.Models;
using LunaDraw.Logic.Services;
using SkiaSharp;
using System.Collections.Concurrent;

namespace LunaDraw.Logic.Utils;

public class DrawingThumbnailHandler : IDrawingThumbnailHandler
{
  private readonly IDrawingStorageMomento drawingStorageMomento;
  private readonly IThumbnailCacheFacade thumbnailCacheFacade;
  private readonly ConcurrentDictionary<Guid, ImageSource> inMemoryImageSourceCache = new();

  public DrawingThumbnailHandler(
    IDrawingStorageMomento drawingStorageMomento,
    IThumbnailCacheFacade thumbnailCacheFacade)
  {
    this.drawingStorageMomento = drawingStorageMomento;
    this.thumbnailCacheFacade = thumbnailCacheFacade;
  }

  public async Task<string?> GetThumbnailBase64Async(Guid drawingId, int width, int height, Logic.Models.External.Drawing? drawing = null)
  {
    // Check persistent cache first
    var cachedBase64 = await thumbnailCacheFacade.GetThumbnailBase64Async(drawingId);
    if (cachedBase64 != null)
    {
      return cachedBase64;
    }

    // Use provided drawing or load from storage
    var drawingToUse = drawing ?? await drawingStorageMomento.LoadDrawingAsync(drawingId);
    if (drawingToUse == null)
    {
      return null;
    }

    var base64 = await GenerateThumbnailBase64Async(drawingToUse, width, height);

    // Save to persistent cache
    if (base64 != null)
    {
      await thumbnailCacheFacade.SaveThumbnailAsync(drawingId, base64);
    }

    return base64;
  }

  public async Task<ImageSource?> GetThumbnailAsync(Guid drawingId, int width, int height)
  {
    // Check in-memory cache first
    if (inMemoryImageSourceCache.TryGetValue(drawingId, out var cachedThumbnail))
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

    // Cache it in memory
    if (thumbnail != null)
    {
      inMemoryImageSourceCache[drawingId] = thumbnail;
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

      foreach (var layer in drawing.Layers.Where(l => l.IsVisible))
      {
        foreach (var element in layer.Elements.Where(e => e.IsVisible).OrderBy(e => e.ZIndex))
        {
          RenderElement(canvas, element);
        }
      }

      // Create image from surface
      using var image = surface.Snapshot();
      using var data = image.Encode(SKEncodedImageFormat.Png, 100);

      if (data == null)
      {
        return Task.FromResult<ImageSource?>(null);
      }

      var imageBytes = data.ToArray();

      if (imageBytes == null || imageBytes.Length == 0)
      {
        return Task.FromResult<ImageSource?>(null);
      }

      var imageSource = ImageSource.FromStream(() => new MemoryStream(imageBytes));

      return Task.FromResult<ImageSource?>(imageSource);
    }
    catch (Exception)
    {
      return Task.FromResult<ImageSource?>(null);
    }
  }

  private Task<string?> GenerateThumbnailBase64Async(Logic.Models.External.Drawing drawing, int width, int height)
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

      foreach (var layer in drawing.Layers.Where(l => l.IsVisible))
      {
        foreach (var element in layer.Elements.Where(e => e.IsVisible).OrderBy(e => e.ZIndex))
        {
          RenderElement(canvas, element);
        }
      }

      // Create image from surface
      using var image = surface.Snapshot();
      using var data = image.Encode(SKEncodedImageFormat.Png, 80);

      if (data == null)
      {
        return Task.FromResult<string?>(null);
      }

      var imageBytes = data.ToArray();

      if (imageBytes == null || imageBytes.Length == 0)
      {
        return Task.FromResult<string?>(null);
      }

      var base64String = Convert.ToBase64String(imageBytes);

      return Task.FromResult<string?>(base64String);
    }
    catch (Exception)
    {
      return Task.FromResult<string?>(null);
    }
  }

  private void RenderElement(SKCanvas canvas, External.Element element)
  {
    using var paint = new SKPaint
    {
      IsAntialias = true
    };

    if (element is External.Path pathElement)
    {
      RenderPath(canvas, pathElement, paint);
    }
    else if (element is External.Stamps stampsElement)
    {
      RenderStamps(canvas, stampsElement, paint);
    }
    else if (element is External.Rectangle rectangleElement)
    {
      RenderRectangle(canvas, rectangleElement, paint);
    }
    else if (element is External.Ellipse ellipseElement)
    {
      RenderEllipse(canvas, ellipseElement, paint);
    }
    else if (element is External.Line lineElement)
    {
      RenderLine(canvas, lineElement, paint);
    }
  }

  private void RenderPath(SKCanvas canvas, External.Path pathElement, SKPaint paint)
  {
    if (string.IsNullOrEmpty(pathElement.PathData))
    {
      return;
    }

    using var path = SKPath.ParseSvgPathData(pathElement.PathData);
    if (path == null)
    {
      return;
    }

    if (!string.IsNullOrEmpty(pathElement.StrokeColor))
    {
      SKColor.TryParse(pathElement.StrokeColor, out var strokeColor);
      paint.Color = strokeColor.WithAlpha(pathElement.Opacity);
      paint.StrokeWidth = pathElement.StrokeWidth;
      paint.Style = SKPaintStyle.Stroke;
      canvas.DrawPath(path, paint);
    }

    if (pathElement.IsFilled && !string.IsNullOrEmpty(pathElement.FillColor))
    {
      SKColor.TryParse(pathElement.FillColor, out var fillColor);
      paint.Color = fillColor.WithAlpha(pathElement.Opacity);
      paint.Style = SKPaintStyle.Fill;
      canvas.DrawPath(path, paint);
    }
  }

  private void RenderStamps(SKCanvas canvas, External.Stamps stampsElement, SKPaint paint)
  {
    if (stampsElement.Points == null || !stampsElement.Points.Any())
    {
      return;
    }

    var shapePath = GetShapePath((BrushShapeType)stampsElement.ShapeType);
    if (shapePath == null)
    {
      return;
    }

    if (!string.IsNullOrEmpty(stampsElement.StrokeColor))
    {
      SKColor.TryParse(stampsElement.StrokeColor, out var strokeColor);
      paint.Color = strokeColor.WithAlpha(stampsElement.Opacity);
    }

    paint.Style = stampsElement.IsFilled ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
    paint.StrokeWidth = stampsElement.StrokeWidth;

    for (int i = 0; i < stampsElement.Points.Count; i++)
    {
      var point = stampsElement.Points[i];
      var position = new SKPoint(point[0], point[1]);

      canvas.Save();
      canvas.Translate(position.X, position.Y);
      canvas.Scale(stampsElement.Size, stampsElement.Size);

      if (stampsElement.Rotations != null && i < stampsElement.Rotations.Count)
      {
        canvas.RotateDegrees(stampsElement.Rotations[i]);
      }

      canvas.DrawPath(shapePath, paint);
      canvas.Restore();
    }
  }

  private void RenderRectangle(SKCanvas canvas, External.Rectangle rectangleElement, SKPaint paint)
  {
    var rect = new SKRect(rectangleElement.Left, rectangleElement.Top, rectangleElement.Right, rectangleElement.Bottom);

    canvas.Save();
    if (rectangleElement.TransformMatrix != null && rectangleElement.TransformMatrix.Length == 9)
    {
      var matrix = new SKMatrix
      {
        ScaleX = rectangleElement.TransformMatrix[0],
        SkewX = rectangleElement.TransformMatrix[1],
        TransX = rectangleElement.TransformMatrix[2],
        SkewY = rectangleElement.TransformMatrix[3],
        ScaleY = rectangleElement.TransformMatrix[4],
        TransY = rectangleElement.TransformMatrix[5],
        Persp0 = rectangleElement.TransformMatrix[6],
        Persp1 = rectangleElement.TransformMatrix[7],
        Persp2 = rectangleElement.TransformMatrix[8]
      };
      canvas.Concat(in matrix);
    }

    if (!string.IsNullOrEmpty(rectangleElement.FillColor))
    {
      SKColor.TryParse(rectangleElement.FillColor, out var fillColor);
      paint.Color = fillColor.WithAlpha(rectangleElement.Opacity);
      paint.Style = SKPaintStyle.Fill;
      canvas.DrawRect(rect, paint);
    }

    if (!string.IsNullOrEmpty(rectangleElement.StrokeColor) && rectangleElement.StrokeWidth > 0)
    {
      SKColor.TryParse(rectangleElement.StrokeColor, out var strokeColor);
      paint.Color = strokeColor.WithAlpha(rectangleElement.Opacity);
      paint.StrokeWidth = rectangleElement.StrokeWidth;
      paint.Style = SKPaintStyle.Stroke;
      canvas.DrawRect(rect, paint);
    }

    canvas.Restore();
  }

  private void RenderEllipse(SKCanvas canvas, External.Ellipse ellipseElement, SKPaint paint)
  {
    var oval = new SKRect(ellipseElement.Left, ellipseElement.Top, ellipseElement.Right, ellipseElement.Bottom);

    canvas.Save();
    if (ellipseElement.TransformMatrix != null && ellipseElement.TransformMatrix.Length == 9)
    {
      var matrix = new SKMatrix
      {
        ScaleX = ellipseElement.TransformMatrix[0],
        SkewX = ellipseElement.TransformMatrix[1],
        TransX = ellipseElement.TransformMatrix[2],
        SkewY = ellipseElement.TransformMatrix[3],
        ScaleY = ellipseElement.TransformMatrix[4],
        TransY = ellipseElement.TransformMatrix[5],
        Persp0 = ellipseElement.TransformMatrix[6],
        Persp1 = ellipseElement.TransformMatrix[7],
        Persp2 = ellipseElement.TransformMatrix[8]
      };
      canvas.Concat(in matrix);
    }

    if (!string.IsNullOrEmpty(ellipseElement.FillColor))
    {
      SKColor.TryParse(ellipseElement.FillColor, out var fillColor);
      paint.Color = fillColor.WithAlpha(ellipseElement.Opacity);
      paint.Style = SKPaintStyle.Fill;
      canvas.DrawOval(oval, paint);
    }

    if (!string.IsNullOrEmpty(ellipseElement.StrokeColor) && ellipseElement.StrokeWidth > 0)
    {
      SKColor.TryParse(ellipseElement.StrokeColor, out var strokeColor);
      paint.Color = strokeColor.WithAlpha(ellipseElement.Opacity);
      paint.StrokeWidth = ellipseElement.StrokeWidth;
      paint.Style = SKPaintStyle.Stroke;
      canvas.DrawOval(oval, paint);
    }

    canvas.Restore();
  }

  private void RenderLine(SKCanvas canvas, External.Line lineElement, SKPaint paint)
  {
    var startPoint = new SKPoint(lineElement.StartX, lineElement.StartY);
    var endPoint = new SKPoint(lineElement.EndX, lineElement.EndY);

    canvas.Save();
    if (lineElement.TransformMatrix != null && lineElement.TransformMatrix.Length == 9)
    {
      var matrix = new SKMatrix
      {
        ScaleX = lineElement.TransformMatrix[0],
        SkewX = lineElement.TransformMatrix[1],
        TransX = lineElement.TransformMatrix[2],
        SkewY = lineElement.TransformMatrix[3],
        ScaleY = lineElement.TransformMatrix[4],
        TransY = lineElement.TransformMatrix[5],
        Persp0 = lineElement.TransformMatrix[6],
        Persp1 = lineElement.TransformMatrix[7],
        Persp2 = lineElement.TransformMatrix[8]
      };
      canvas.Concat(in matrix);
    }

    if (!string.IsNullOrEmpty(lineElement.StrokeColor) && lineElement.StrokeWidth > 0)
    {
      SKColor.TryParse(lineElement.StrokeColor, out var strokeColor);
      paint.Color = strokeColor.WithAlpha(lineElement.Opacity);
      paint.StrokeWidth = lineElement.StrokeWidth;
      paint.Style = SKPaintStyle.Stroke;
      canvas.DrawLine(startPoint, endPoint, paint);
    }

    canvas.Restore();
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

  public async Task InvalidateThumbnailAsync(Guid drawingId)
  {
    inMemoryImageSourceCache.TryRemove(drawingId, out _);
    await thumbnailCacheFacade.InvalidateThumbnailAsync(drawingId);
  }

  public async Task ClearCacheAsync()
  {
    inMemoryImageSourceCache.Clear();
    await thumbnailCacheFacade.ClearAllCacheAsync();
  }
}
