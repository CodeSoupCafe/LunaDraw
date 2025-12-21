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

using System.Text.Json;
using System.Threading;
using LunaDraw.Logic.Models;
using SkiaSharp;
using System.Text.Json.Serialization;
using LunaDraw.Logic.Constants;

namespace LunaDraw.Logic.Utils;

public interface IDrawingStorageMomento
{
  Task<List<External.Drawing>> LoadAllDrawingsAsync();
  Task<External.Drawing?> LoadDrawingAsync(Guid id);
  Task ExternalDrawingAsync(External.Drawing drawing);
  Task DeleteDrawingAsync(Guid id);
  Task DuplicateDrawingAsync(Guid id);
  Task RenameDrawingAsync(Guid id, string newName);
  Task RenameUntitledDrawingsAsync();

  // Conversion helpers
  External.Drawing CreateExternalDrawingFromCurrent(IEnumerable<Layer> layers, int width, int height, string name, Guid id);
  List<Layer> RestoreLayers(External.Drawing externalDrawing);
  Task<string> GetNextDefaultNameAsync();
}

public class DrawingStorageMomento : IDrawingStorageMomento
{
  private readonly string storagePath;
  private readonly JsonSerializerOptions jsonOptions;
  private const string DefaultNamePrefix = "Drawing ";
  private readonly SemaphoreSlim fileLock = new(1, 1);

  public DrawingStorageMomento(string? storagePath = null)
  {
    this.storagePath = storagePath ?? Path.Combine(FileSystem.AppDataDirectory, AppConstants.Directories.Gallery);
    if (!Directory.Exists(this.storagePath))
    {
      Directory.CreateDirectory(this.storagePath);
    }

    jsonOptions = new JsonSerializerOptions
    {
      WriteIndented = true,
      PropertyNameCaseInsensitive = true,
      // Configure polymorphism for externalDrawing
      Converters = { new JsonStringEnumConverter() } // For MaskingMode and BlendMode if they are enums
    };
    // Add resolver for polymorphism if not using JsonDerivedType directly
    // The JsonDerivedType attribute on externalDrawingZz handles this.
  }

  public async Task<string> GetNextDefaultNameAsync()
  {
    try
    {
      var drawings = await LoadAllDrawingsAsync();
      var numbers = drawings
          .Where(d => d.Name.StartsWith(DefaultNamePrefix))
          .Select(d =>
          {
            if (int.TryParse(d.Name.Substring(DefaultNamePrefix.Length), out int result))
              return result;
            return 0;
          })
          .DefaultIfEmpty(0)
          .ToList();

      return $"{DefaultNamePrefix}{numbers.Max() + 1}";
    }
    catch
    {
      return $"{DefaultNamePrefix}1";
    }
  }

  public async Task<List<External.Drawing>> LoadAllDrawingsAsync()
  {
    var drawings = new List<External.Drawing>();
    var files = Directory.GetFiles(storagePath, AppConstants.Files.JsonSearchPattern);

    foreach (var file in files)
    {
      try
      {
        var json = await File.ReadAllTextAsync(file);
        var drawing = JsonSerializer.Deserialize<External.Drawing>(json, jsonOptions);
        if (drawing != null)
        {
          drawings.Add(drawing);
        }
      }
      catch (Exception)
      {
      }
    }

    return drawings.OrderByDescending(d => d.LastModified).ToList();
  }

  public async Task<External.Drawing?> LoadDrawingAsync(Guid id)
  {
    var path = Path.Combine(storagePath, $"{id}{AppConstants.Files.JsonExtension}");
    if (!File.Exists(path)) return null;

    try
    {
      var json = await File.ReadAllTextAsync(path);
      return JsonSerializer.Deserialize<External.Drawing>(json, jsonOptions);
    }
    catch (Exception)
    {
      return null;
    }
  }

  public async Task ExternalDrawingAsync(External.Drawing drawing)
  {
    await fileLock.WaitAsync();
    try
    {
      drawing.LastModified = DateTime.Now;
      var path = Path.Combine(storagePath, $"{drawing.Id}{AppConstants.Files.JsonExtension}");
      var json = JsonSerializer.Serialize(drawing, jsonOptions);
      await File.WriteAllTextAsync(path, json);
    }
    finally
    {
      fileLock.Release();
    }
  }

  public Task DeleteDrawingAsync(Guid id)
  {
    var path = Path.Combine(storagePath, $"{id}{AppConstants.Files.JsonExtension}");
    if (File.Exists(path))
    {
      File.Delete(path);
    }
    return Task.CompletedTask;
  }

  public async Task DuplicateDrawingAsync(Guid id)
  {
    var original = await LoadDrawingAsync(id);
    if (original == null) return;

    original.Id = Guid.NewGuid();
    original.Name = $"{original.Name} (Copy)";
    original.LastModified = DateTime.Now;

    // Ensure layers also have unique IDs if needed, though they are usually scoped to drawing
    // But let's keep them as is for now, assuming Layer IDs are not globally unique requirements across drawings

    await ExternalDrawingAsync(original);
  }

  public async Task RenameDrawingAsync(Guid id, string newName)
  {
    var drawing = await LoadDrawingAsync(id);
    if (drawing == null) return;

    drawing.Name = newName;
    await ExternalDrawingAsync(drawing);
  }

  public async Task RenameUntitledDrawingsAsync()
  {
    var drawings = await LoadAllDrawingsAsync();
    var untitledDrawings = drawings.Where(d => d.Name == "Untitled").OrderBy(d => d.LastModified).ToList();

    if (!untitledDrawings.Any())
    {
      return;
    }

    // Get the next available number
    var existingNumbers = drawings
        .Where(d => d.Name.StartsWith(DefaultNamePrefix))
        .Select(d =>
        {
          if (int.TryParse(d.Name.Substring(DefaultNamePrefix.Length), out int result))
            return result;
          return 0;
        })
        .DefaultIfEmpty(0)
        .ToList();

    int nextNumber = existingNumbers.Max() + 1;

    foreach (var drawing in untitledDrawings)
    {
      drawing.Name = $"{DefaultNamePrefix}{nextNumber}";
      await ExternalDrawingAsync(drawing);
      nextNumber++;
    }
  }

  #region Conversion Helpers

  private static BrushShape GetBrushShapeStatic(BrushShapeType type)
  {
      return type switch
      {
          BrushShapeType.Circle => BrushShape.Circle(),
          BrushShapeType.Square => BrushShape.Square(),
          BrushShapeType.Star => BrushShape.Star(),
          BrushShapeType.Heart => BrushShape.Heart(),
          BrushShapeType.Sparkle => BrushShape.Sparkle(),
          BrushShapeType.Cloud => BrushShape.Cloud(),
          BrushShapeType.Moon => BrushShape.Moon(),
          BrushShapeType.Lightning => BrushShape.Lightning(),
          BrushShapeType.Diamond => BrushShape.Diamond(),
          BrushShapeType.Triangle => BrushShape.Triangle(),
          BrushShapeType.Hexagon => BrushShape.Hexagon(),
          BrushShapeType.Unicorn => BrushShape.Unicorn(),
          BrushShapeType.Giraffe => BrushShape.Giraffe(),
          BrushShapeType.Bear => BrushShape.Bear(),
          BrushShapeType.Elephant => BrushShape.Elephant(),
          BrushShapeType.Tiger => BrushShape.Tiger(),
          BrushShapeType.Monkey => BrushShape.Monkey(),
          BrushShapeType.Fireworks => BrushShape.Fireworks(),
          BrushShapeType.Flower => BrushShape.Flower(),
          BrushShapeType.Sun => BrushShape.Sun(),
          BrushShapeType.Snowflake => BrushShape.Snowflake(),
          BrushShapeType.Butterfly => BrushShape.Butterfly(),
          BrushShapeType.Fish => BrushShape.Fish(),
          BrushShapeType.Paw => BrushShape.Paw(),
          BrushShapeType.Leaf => BrushShape.Leaf(),
          BrushShapeType.MusicNote => BrushShape.MusicNote(),
          BrushShapeType.Smile => BrushShape.Smile(),
          _ => BrushShape.Circle()
      };
  }

  public External.Drawing CreateExternalDrawingFromCurrent(IEnumerable<Layer> layers, int width, int height, string name, Guid id)
  {
      return CreateExternalDrawingFromCurrentStatic(layers, width, height, name, id);
  }

  public static External.Drawing CreateExternalDrawingFromCurrentStatic(IEnumerable<Layer> layers, int width, int height, string name, Guid id)
  {
    var externalDrawing = new External.Drawing
    {
      Id = id,
      Name = name,
      LastModified = DateTime.Now,
      CanvasWidth = width,
      CanvasHeight = height
    };

    foreach (var layer in layers)
    {
      var externalLayer = new External.Layer
      {
        Id = layer.Id,
        Name = layer.Name,
        IsVisible = layer.IsVisible,
        IsLocked = layer.IsLocked,
        MaskingMode = (int)layer.MaskingMode
      };

      foreach (var element in layer.Elements)
      {
        External.Element? externelElement = null;

        if (element is DrawablePath drawablePath)
        {
          externelElement = new External.Path
          {
            PathData = drawablePath.Path?.ToSvgPathData() ?? string.Empty,
            IsFilled = drawablePath.IsFilled,
            BlendMode = (int)drawablePath.BlendMode
          };
        }
        else if (element is DrawableRectangle drawableRect)
        {
          using var path = new SKPath();
          path.AddRect(drawableRect.Rectangle);
          externelElement = new External.Path
          {
            PathData = path.ToSvgPathData(),
            IsFilled = drawableRect.FillColor.HasValue,
            BlendMode = (int)SKBlendMode.SrcOver
          };
        }
        else if (element is DrawableEllipse drawableEllipse)
        {
          using var path = new SKPath();
          path.AddOval(drawableEllipse.Oval);
          externelElement = new External.Path
          {
            PathData = path.ToSvgPathData(),
            IsFilled = drawableEllipse.FillColor.HasValue,
            BlendMode = (int)SKBlendMode.SrcOver
          };
        }
        else if (element is DrawableLine drawableLine)
        {
          using var path = new SKPath();
          path.MoveTo(drawableLine.StartPoint);
          path.LineTo(drawableLine.EndPoint);
          externelElement = new External.Path
          {
            PathData = path.ToSvgPathData(),
            IsFilled = false,
            BlendMode = (int)SKBlendMode.SrcOver
          };
        }
        else if (element is DrawableStamps drawableStamps)
        {
            var points = drawableStamps.Points.Select(p => new float[] { p.X, p.Y }).ToList();
            externelElement = new External.Stamps
            {
                Points = points,
                ShapeType = (int)drawableStamps.Shape.Type,
                Size = drawableStamps.Size,
                Flow = drawableStamps.Flow,
                IsFilled = drawableStamps.IsFilled,
                BlendMode = (int)drawableStamps.BlendMode,
                IsRainbowEnabled = drawableStamps.IsRainbowEnabled,
                Rotations = drawableStamps.Rotations,
                SizeJitter = drawableStamps.SizeJitter,
                AngleJitter = drawableStamps.AngleJitter,
                HueJitter = drawableStamps.HueJitter
            };
        }

        if (externelElement != null)
        {
          // Common properties
          externelElement.Id = element.Id;
          externelElement.IsVisible = element.IsVisible;
          externelElement.ZIndex = element.ZIndex;
          externelElement.Opacity = element.Opacity;
          externelElement.FillColor = element.FillColor?.ToString(); // Use ToHex() extension
          externelElement.StrokeColor = element.StrokeColor.ToString() ?? SKColors.Black.ToString(); 
          externelElement.StrokeWidth = element.StrokeWidth;
          externelElement.IsGlowEnabled = element.IsGlowEnabled;
          externelElement.GlowColor = element.GlowColor.ToString() ?? SKColors.Transparent.ToString();
          externelElement.GlowRadius = element.GlowRadius;

          externelElement.TransformMatrix = new float[9];
          element.TransformMatrix.GetValues(externelElement.TransformMatrix);

          externalLayer.Elements.Add(externelElement);
        }
      }

      externalDrawing.Layers.Add(externalLayer);
    }

    return externalDrawing;
  }

  public List<Layer> RestoreLayers(External.Drawing savedDrawing)
  {
      return RestoreLayersStatic(savedDrawing);
  }

  public static List<Layer> RestoreLayersStatic(External.Drawing savedDrawing)
  {
    var layers = new List<Layer>();

    foreach (var savedLayer in savedDrawing.Layers)
    {
      var layer = new Layer
      {
        Id = savedLayer.Id, // Set the Id from the saved layer
        Name = savedLayer.Name,
        IsVisible = savedLayer.IsVisible,
        IsLocked = savedLayer.IsLocked,
        MaskingMode = (MaskingMode)savedLayer.MaskingMode
      };

      // Order by ZIndex
      var sortedElements = savedLayer.Elements.OrderBy(e => e.ZIndex);

      foreach (var savedElement in sortedElements)
      {
        try
        {
            IDrawableElement? element = null;

            if (savedElement is External.Path savedPath)
            {
              element = new DrawablePath
              {
                Id = savedElement.Id, // Set the Id from the saved element
                Path = SKPath.ParseSvgPathData(savedPath.PathData),
                IsFilled = savedPath.IsFilled,
                BlendMode = (SKBlendMode)savedPath.BlendMode
              };
            }
            else if (savedElement is External.Stamps savedStamps)
            {
                var points = savedStamps.Points?.Select(p => new SKPoint(p[0], p[1])).ToList() ?? new List<SKPoint>();
                var brushShape = GetBrushShapeStatic((BrushShapeType)savedStamps.ShapeType);
                
                element = new DrawableStamps
                {
                    Id = savedElement.Id,
                    Points = points,
                    Shape = brushShape,
                    Size = savedStamps.Size,
                    Flow = savedStamps.Flow,
                    IsFilled = savedStamps.IsFilled,
                    BlendMode = (SKBlendMode)savedStamps.BlendMode,
                    IsRainbowEnabled = savedStamps.IsRainbowEnabled,
                    Rotations = savedStamps.Rotations,
                    SizeJitter = savedStamps.SizeJitter,
                    AngleJitter = savedStamps.AngleJitter,
                    HueJitter = savedStamps.HueJitter
                };
            }

            if (element != null)
            {
              // Common properties
              element.IsVisible = savedElement.IsVisible;
              element.ZIndex = savedElement.ZIndex;
              element.Opacity = savedElement.Opacity;
              
              if (!string.IsNullOrEmpty(savedElement.FillColor))
              {
                SKColor.TryParse(savedElement.FillColor, out var fillColor);
                element.FillColor = fillColor;
              }
              else
              {
                element.FillColor = null;
              }

              if (!string.IsNullOrEmpty(savedElement.StrokeColor))
              {
                SKColor.TryParse(savedElement.StrokeColor, out var strokeColor);
                element.StrokeColor = strokeColor;
              }
              else
              {
                 // Default fallback if missing
                 element.StrokeColor = SKColors.Black;
              }
              
              element.StrokeWidth = savedElement.StrokeWidth;

              element.IsGlowEnabled = savedElement.IsGlowEnabled;
              
              if (!string.IsNullOrEmpty(savedElement.GlowColor))
              {
                SKColor.TryParse(savedElement.GlowColor, out var glowColor);
                element.GlowColor = glowColor;
              }
              else
              {
                 element.GlowColor = SKColors.Transparent;
              }
              
              element.GlowRadius = savedElement.GlowRadius;

              if (savedElement.TransformMatrix != null && savedElement.TransformMatrix.Length == 9)
              {
                  var matValues = savedElement.TransformMatrix;
                  var matrix = new SKMatrix
                  {
                    ScaleX = matValues[0],
                    SkewX = matValues[1],
                    TransX = matValues[2],
                    SkewY = matValues[3],
                    ScaleY = matValues[4],
                    TransY = matValues[5],
                    Persp0 = matValues[6],
                    Persp1 = matValues[7],
                    Persp2 = matValues[8]
                  };
                  element.TransformMatrix = matrix;
              }

              layer.Elements.Add(element);
            }
        }
        catch (Exception)
        {
        }
      }
      // Re-attach event handler after population IF `Elements` was not passed in constructor
      // The Layer constructor already adds Elements.CollectionChanged -= OnElementsCollectionChanged;
      // which means if it's called internally, it re-attaches it. For external population, it won't.
      // For now, assuming Layer's constructor and property handling is sufficient.
      layers.Add(layer);
    }

    return layers;
  }

  #endregion
}
