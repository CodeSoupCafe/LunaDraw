using System.Collections.ObjectModel;
using System.Collections.Specialized;
using LunaDraw.Logic.Utils;
using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Models
{
  /// <summary>
  /// Represents a layer in the drawing, containing a collection of drawable elements.
  /// Uses QuadTree for spatial indexing and simple culling for performance.
  /// NO BITMAP TILING.
  /// </summary>
  public class Layer : ReactiveObject
  {
    private string _name = "Layer";
    private bool _isVisible = true;
    private bool _isLocked = false;
    
    private QuadTree<IDrawableElement> _quadTree;

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
         // No cache to invalidate
    }
    
    public void Draw(SKCanvas canvas)
    {
        // Get visible rect in World Coordinates (Local Clip Bounds handles the matrix transform automatically)
        var visibleRect = canvas.LocalClipBounds;

        // Use QuadTree to find elements that are potentially visible
        var visibleElements = new List<IDrawableElement>();
        _quadTree.Retrieve(visibleElements, visibleRect);
        
        // Sort by ZIndex to ensure correct draw order
        visibleElements.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));
        
        foreach (var element in visibleElements)
        {
             if (element.IsVisible)
             {
                 // Double check intersection just in case QuadTree is loose
                 if (element.Bounds.IntersectsWith(visibleRect))
                 {
                     element.Draw(canvas);
                 }
             }
        }
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