using System.Collections.ObjectModel;
using System.Collections.Specialized;
using LunaDraw.Logic.Utils;
using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Models
{
  /// <summary>
  /// Represents a layer in the drawing, containing a collection of drawable elements.
  /// Uses QuadTree for spatial indexing and Tiled Rendering for performance.
  /// </summary>
  public class Layer : ReactiveObject
  {
    private string _name = "Layer";
    private bool _isVisible = true;
    private bool _isLocked = false;
    
    private QuadTree<IDrawableElement> _quadTree;
    private readonly Dictionary<(int x, int y), SKBitmap> _tiles = [];
    private const int TileSize = 512;

    public Guid Id { get; } = Guid.NewGuid();

    public Layer()
    {
        // Initialize QuadTree with large bounds (arbitrary large world)
        var worldBounds = new SKRect(-500000, -500000, 500000, 500000);
        _quadTree = new QuadTree<IDrawableElement>(0, worldBounds, e => e.Bounds);

        Elements.CollectionChanged += OnElementsCollectionChanged;
    }

    public string Name
    {
      get => _name;
      set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public ObservableCollection<IDrawableElement> Elements { get; } = [];

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    public bool IsLocked
    {
      get => _isLocked;
      set => this.RaiseAndSetIfChanged(ref _isLocked, value);
    }
    
    private void OnElementsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Assign ZIndex to new items based on current count to maintain draw order
        if (e.NewItems != null)
        {
            int startCount = Elements.Count - e.NewItems.Count;
            foreach (IDrawableElement item in e.NewItems)
            {
                if (item.ZIndex == 0) item.ZIndex = startCount++;
            }
        }

        RebuildQuadTree();
        
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (IDrawableElement item in e.NewItems)
            {
                InvalidateTiles(item.Bounds);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (IDrawableElement item in e.OldItems)
            {
                InvalidateTiles(item.Bounds);
            }
        }
        else
        {
            InvalidateAllTiles();
        }
    }
    
    private void RebuildQuadTree()
    {
        _quadTree.Clear();
        foreach (var element in Elements)
        {
            _quadTree.Insert(element);
        }
    }
    
    public void InvalidateCache()
    {
         InvalidateAllTiles();
    }
    
    private void InvalidateTiles(SKRect bounds)
    {
        var minX = (int)Math.Floor(bounds.Left / TileSize);
        var minY = (int)Math.Floor(bounds.Top / TileSize);
        var maxX = (int)Math.Ceiling(bounds.Right / TileSize);
        var maxY = (int)Math.Ceiling(bounds.Bottom / TileSize);

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                if (_tiles.TryGetValue((x, y), out var bitmap))
                {
                    bitmap.Dispose();
                    _tiles.Remove((x, y));
                }
            }
        }
    }

    private void InvalidateAllTiles()
    {
        foreach (var bitmap in _tiles.Values)
        {
            bitmap.Dispose();
        }
        _tiles.Clear();
    }
    
    public void Draw(SKCanvas canvas)
    {
        if (!canvas.TotalMatrix.TryInvert(out var inverse))
            return; 

        // Get visible rect in world coordinates
        var visibleRect = canvas.LocalClipBounds;
        
        var minX = (int)Math.Floor(visibleRect.Left / TileSize);
        var minY = (int)Math.Floor(visibleRect.Top / TileSize);
        var maxX = (int)Math.Ceiling(visibleRect.Right / TileSize);
        var maxY = (int)Math.Ceiling(visibleRect.Bottom / TileSize);

        using var paint = new SKPaint(); 
        
        // 1. Draw Cached Tiles
        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                var key = (x, y);
                if (!_tiles.TryGetValue(key, out var tileBitmap))
                {
                    tileBitmap = RenderTile(x, y);
                    _tiles[key] = tileBitmap;
                }
                
                canvas.DrawBitmap(tileBitmap, x * TileSize, y * TileSize, paint);
            }
        }

        // 2. Draw Selected Elements (Live)
        // Query QuadTree for visible elements, then filter for IsSelected
        var visibleElements = new List<IDrawableElement>();
        _quadTree.Retrieve(visibleElements, visibleRect);
        
        foreach (var element in visibleElements)
        {
             if (element.IsVisible && element.IsSelected)
             {
                 element.Draw(canvas);
             }
        }
    }
    
    private SKBitmap RenderTile(int x, int y)
    {
        var bitmap = new SKBitmap(TileSize, TileSize);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Translate(-x * TileSize, -y * TileSize);
        
        var tileRect = new SKRect(x * TileSize, y * TileSize, (x + 1) * TileSize, (y + 1) * TileSize);
        
        var elementsInTile = new List<IDrawableElement>();
        _quadTree.Retrieve(elementsInTile, tileRect);
        
        // Sort by ZIndex for correct layering within the tile
        elementsInTile.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));

        foreach (var element in elementsInTile)
        {
            if (element.IsVisible && !element.IsSelected)
            {
                if (element.Bounds.IntersectsWith(tileRect))
                {
                    element.Draw(canvas);
                }
            }
        }
        
        return bitmap;
    }

    public Layer Clone()
    {
      var clone = new Layer
      {
        Name = Name,
        IsVisible = IsVisible,
        IsLocked = IsLocked
      };

      foreach (var element in Elements)
      {
        clone.Elements.Add(element.Clone());
      }

      return clone;
    }
  }
}
