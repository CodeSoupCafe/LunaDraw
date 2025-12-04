using System.Collections.ObjectModel;

using LunaDraw.Logic.Models;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Managers
{
  public class SelectionManager : ReactiveObject
  {
    private readonly ObservableCollection<IDrawableElement> selected = [];
    public ReadOnlyObservableCollection<IDrawableElement> Selected { get; }

    public SelectionManager()
    {
      Selected = new ReadOnlyObservableCollection<IDrawableElement>(selected);
    }

    public void Clear()
    {
      if (selected.Count == 0) return;

      var elementsToClear = selected.ToList();
      selected.Clear();

      foreach (var el in elementsToClear)
      {
        el.IsSelected = false;
      }

      OnSelectionChanged();
    }

    public void Add(IDrawableElement element)
    {
      if (element == null || selected.Contains(element)) return;
      element.IsSelected = true;
      selected.Add(element);
      OnSelectionChanged();
    }

    public void AddRange(IEnumerable<IDrawableElement> elements)
    {
      var changed = false;
      foreach (var element in elements)
      {
        if (element != null && !selected.Contains(element))
        {
          element.IsSelected = true;
          selected.Add(element);
          changed = true;
        }
      }

      if (changed)
        OnSelectionChanged();
    }

    public void Remove(IDrawableElement element)
    {
      if (element == null || !selected.Contains(element)) return;
      element.IsSelected = false;
      selected.Remove(element);
      OnSelectionChanged();
    }

    public void Toggle(IDrawableElement element)
    {
      if (element == null) return;

      if (selected.Contains(element))
      {
        Remove(element);
      }
      else
      {
        Add(element);
      }
    }

    public bool Contains(IDrawableElement element)
    {
      return element != null && selected.Contains(element);
    }

    public IReadOnlyList<IDrawableElement> GetAll()
    {
      return selected.ToList().AsReadOnly();
    }

    public SKRect GetBounds()
    {
      if (selected.Count == 0)
      {
        return SKRect.Empty;
      }

      var bounds = selected[0].Bounds;
      for (var i = 1; i < selected.Count; i++)
      {
        bounds.Union(selected[i].Bounds);
      }

      return bounds;
    }

    private void OnSelectionChanged()
    {
      this.RaisePropertyChanged(nameof(Bounds));
      this.RaisePropertyChanged(nameof(HasSelection));
      SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? SelectionChanged;

    public SKRect Bounds => GetBounds();
    public bool HasSelection => selected.Count > 0;
  }
}