using System.Collections.ObjectModel;

using LunaDraw.Logic.Models;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Managers
{
  public class SelectionManager : ReactiveObject
  {
    private readonly ObservableCollection<IDrawableElement> _selected = [];
    public ReadOnlyObservableCollection<IDrawableElement> Selected { get; }

    public SelectionManager()
    {
      Selected = new ReadOnlyObservableCollection<IDrawableElement>(_selected);
    }

    public void Clear()
    {
      if (_selected.Count == 0) return;

      var elementsToClear = _selected.ToList();
      _selected.Clear();

      foreach (var el in elementsToClear)
      {
        el.IsSelected = false;
      }

      OnSelectionChanged();
    }

    public void Add(IDrawableElement element)
    {
      if (element == null || _selected.Contains(element)) return;
      element.IsSelected = true;
      _selected.Add(element);
      OnSelectionChanged();
    }

    public void AddRange(IEnumerable<IDrawableElement> elements)
    {
      var changed = false;
      foreach (var element in elements)
      {
        if (element != null && !_selected.Contains(element))
        {
          element.IsSelected = true;
          _selected.Add(element);
          changed = true;
        }
      }

      if (changed)
        OnSelectionChanged();
    }

    public void Remove(IDrawableElement element)
    {
      if (element == null || !_selected.Contains(element)) return;
      element.IsSelected = false;
      _selected.Remove(element);
      OnSelectionChanged();
    }

    public void Toggle(IDrawableElement element)
    {
      if (element == null) return;

      if (_selected.Contains(element))
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
      return element != null && _selected.Contains(element);
    }

    public IReadOnlyList<IDrawableElement> GetAll()
    {
      return _selected.ToList().AsReadOnly();
    }

    public SKRect GetBounds()
    {
      if (_selected.Count == 0)
      {
        return SKRect.Empty;
      }

      var bounds = _selected[0].Bounds;
      for (var i = 1; i < _selected.Count; i++)
      {
        bounds.Union(_selected[i].Bounds);
      }

      return bounds;
    }

    private void OnSelectionChanged()
    {
      this.RaisePropertyChanged(nameof(Bounds));
      this.RaisePropertyChanged(nameof(HasSelection));
    }

    public SKRect Bounds => GetBounds();
    public bool HasSelection => _selected.Count > 0;
  }
}
