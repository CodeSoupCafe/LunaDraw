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
    private string name = "Layer";
    private bool isVisible = true;
    private bool isLocked = false;
    private MaskingMode maskingMode = MaskingMode.None;
    
    private QuadTree<IDrawableElement> quadTree;

    public Guid Id { get; } = Guid.NewGuid();

    public Layer()
    {
        // Initialize QuadTree with large bounds (arbitrary large world)
        var worldBounds = new SKRect(-500000, -500000, 500000, 500000);
        quadTree = new QuadTree<IDrawableElement>(0, worldBounds, e => e.Bounds);

        Elements.CollectionChanged += OnElementsCollectionChanged;
    }

    public string Name
    {
      get => name;
      set => this.RaiseAndSetIfChanged(ref name, value);
    }

    public ObservableCollection<IDrawableElement> Elements { get; } = [];

    public bool IsVisible
    {
      get => isVisible;
      set => this.RaiseAndSetIfChanged(ref isVisible, value);
    }

    public bool IsLocked
    {
      get => isLocked;
      set => this.RaiseAndSetIfChanged(ref isLocked, value);
    }

    public MaskingMode MaskingMode
    {
      get => maskingMode;
      set => this.RaiseAndSetIfChanged(ref maskingMode, value);
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
        quadTree.Clear();
        foreach (var element in Elements)
        {
            quadTree.Insert(element);
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
        quadTree.Retrieve(visibleElements, visibleRect);
        
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
        IsLocked = IsLocked,
        MaskingMode = MaskingMode
      };

      foreach (var element in Elements)
      {
        clone.Elements.Add(element.Clone());
      }

      return clone;
    }
  }
}
