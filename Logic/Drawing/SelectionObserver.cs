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

using System.Collections.ObjectModel;

using LunaDraw.Logic.Models;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Drawing;

public class SelectionObserver : ReactiveObject
{
  private readonly ObservableCollection<IDrawableElement> selected = [];
  public ReadOnlyObservableCollection<IDrawableElement> Selected { get; }

  public SelectionObserver()
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