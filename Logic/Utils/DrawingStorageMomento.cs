using System.Text.Json;
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

  public DrawingStorageMomento(string? storagePath = null)
  {
    this.storagePath = storagePath ?? Path.Combine(FileSystem.AppDataDirectory, AppConstants.Directories.Gallery);
    if (!Directory.Exists(this.storagePath))
    {
      Directory.CreateDirectory(storagePath);
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
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error loading drawing {file}: {ex.Message}");
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
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Error loading drawing {id}: {ex.Message}");
      return null;
    }
  }

  public async Task ExternalDrawingAsync(External.Drawing drawing)
  {
    drawing.LastModified = DateTime.Now;
    var path = Path.Combine(storagePath, $"{drawing.Id}{AppConstants.Files.JsonExtension}");
    var json = JsonSerializer.Serialize(drawing, jsonOptions);
    await File.WriteAllTextAsync(path, json);
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

  #region Conversion Helpers

  public External.Drawing CreateExternalDrawingFromCurrent(IEnumerable<Layer> layers, int width, int height, string name, Guid id)
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
        // Add other types here when implemented

        if (externelElement != null)
        {
          // Common properties
          externelElement.Id = element.Id;
          externelElement.IsVisible = element.IsVisible;
          externelElement.ZIndex = element.ZIndex;
          externelElement.Opacity = element.Opacity;
          externelElement.FillColor = element.FillColor?.ToString(); // Use ToHex() extension
          externelElement.StrokeColor = element.StrokeColor.ToString(); // Use ToHex() extension
          externelElement.StrokeWidth = element.StrokeWidth;
          externelElement.IsGlowEnabled = element.IsGlowEnabled;
          externelElement.GlowColor = element.GlowColor.ToString(); // Use ToHex() extension
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

          SKColor.TryParse(savedElement.StrokeColor, out var strokeColor);
          element.StrokeColor = strokeColor;
          element.StrokeWidth = savedElement.StrokeWidth;

          element.IsGlowEnabled = savedElement.IsGlowEnabled;
          SKColor.TryParse(savedElement.GlowColor, out var glowColor);
          element.GlowColor = glowColor;
          element.GlowRadius = savedElement.GlowRadius;

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

          layer.Elements.Add(element);
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
