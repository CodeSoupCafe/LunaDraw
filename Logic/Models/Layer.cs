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
        if (e.NewItems != null)
        {
            // Find the maximum existing ZIndex in the layer from elements whose ZIndex is not 0
            // This caters to cases where ZIndex might have been explicitly set (non-zero)
            int maxZIndex = -1; // Default to -1 so first element gets ZIndex 0
            if (Elements.Any())
            {
                maxZIndex = Elements.Where(el => !e.NewItems.Contains(el)) // Exclude newly added items themselves
                                    .DefaultIfEmpty(new DrawablePath { ZIndex = -1, Path = new SKPath() }) // Provide a default if no other elements
                                    .Max(el => el.ZIndex);
            }

            foreach (IDrawableElement item in e.NewItems)
            {
                // Only assign ZIndex if it hasn't been explicitly set (i.e., it's default 0)
                if (item.ZIndex == 0)
                {
                    item.ZIndex = maxZIndex + 1; // Assign a ZIndex higher than any existing element
                    maxZIndex = item.ZIndex; // Update maxZIndex for subsequent new items in this batch
                }
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
