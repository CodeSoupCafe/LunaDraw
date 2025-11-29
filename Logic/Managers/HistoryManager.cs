using LunaDraw.Logic.Models;
using ReactiveUI;

namespace LunaDraw.Logic.Managers
{
  public class HistoryManager : ReactiveObject
  {
    private readonly List<List<Layer>> _history = [];
    private int _historyIndex = -1;

    public bool CanUndo => _historyIndex > 0;
    public bool CanRedo => _historyIndex < _history.Count - 1;

    public void SaveState(IEnumerable<Layer> layers)
    {
      // If we have undone, and then make a new action, we clear the 'redo' history
      if (_historyIndex < _history.Count - 1)
      {
        _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
      }

      // Deep copy the layers
      var stateSnapshot = layers.Select(l => l.Clone()).ToList();
      _history.Add(stateSnapshot);
      _historyIndex++;

      this.RaisePropertyChanged(nameof(CanUndo));
      this.RaisePropertyChanged(nameof(CanRedo));
    }

    public List<Layer>? Undo()
    {
      if (!CanUndo) return null;
      _historyIndex--;

      this.RaisePropertyChanged(nameof(CanUndo));
      this.RaisePropertyChanged(nameof(CanRedo));

      // Return a deep copy of the state to ensure history integrity
      return _history[_historyIndex].Select(l => l.Clone()).ToList();
    }

    public List<Layer>? Redo()
    {
      if (!CanRedo) return null;
      _historyIndex++;

      this.RaisePropertyChanged(nameof(CanUndo));
      this.RaisePropertyChanged(nameof(CanRedo));

      // Return a deep copy of the state
      return _history[_historyIndex].Select(l => l.Clone()).ToList();
    }

    public void Clear()
    {
      _history.Clear();
      _historyIndex = -1;

      this.RaisePropertyChanged(nameof(CanUndo));
      this.RaisePropertyChanged(nameof(CanRedo));
    }
  }
}