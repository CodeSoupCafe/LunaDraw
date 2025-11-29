using System.Collections.ObjectModel;
using ReactiveUI;

namespace LunaDraw.Logic.Models
{
  /// <summary>
  /// Represents a layer in the drawing, containing a collection of drawable elements.
  /// </summary>
  public class Layer : ReactiveObject
  {
    private string _name = "Layer";
    private bool _isVisible = true;
    private bool _isLocked = false;

    public Guid Id { get; } = Guid.NewGuid();

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
