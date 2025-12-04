using LunaDraw.Logic.Models;
using ReactiveUI;

namespace LunaDraw.Logic.Managers
{
  public class HistoryManager : ReactiveObject
  {
    private readonly List<List<Layer>> history = [];
    private int historyIndex = -1;

    public bool CanUndo => historyIndex > 0;
    public bool CanRedo => historyIndex < history.Count - 1;

    public void SaveState(IEnumerable<Layer> layers)
    {
      // If we have undone, and then make a new action, we clear the 'redo' history
      if (historyIndex < history.Count - 1)
      {
        history.RemoveRange(historyIndex + 1, history.Count - (historyIndex + 1));
      }

      // Deep copy the layers
      var stateSnapshot = layers.Select(l => l.Clone()).ToList();
      history.Add(stateSnapshot);
      historyIndex++;

      this.RaisePropertyChanged(nameof(CanUndo));
      this.RaisePropertyChanged(nameof(CanRedo));
    }

    public List<Layer>? Undo()
    {
      if (!CanUndo) return null;
      historyIndex--;

      this.RaisePropertyChanged(nameof(CanUndo));
      this.RaisePropertyChanged(nameof(CanRedo));

      // Return a deep copy of the state to ensure history integrity
      return history[historyIndex].Select(l => l.Clone()).ToList();
    }

    public List<Layer>? Redo()
    {
      if (!CanRedo) return null;
      historyIndex++;

      this.RaisePropertyChanged(nameof(CanUndo));
      this.RaisePropertyChanged(nameof(CanRedo));

      // Return a deep copy of the state
      return history[historyIndex].Select(l => l.Clone()).ToList();
    }

    public void Clear()
    {
      history.Clear();
      historyIndex = -1;

      this.RaisePropertyChanged(nameof(CanUndo));
      this.RaisePropertyChanged(nameof(CanRedo));
    }
  }
}
