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

using LunaDraw.Logic.Models;
using ReactiveUI;

namespace LunaDraw.Logic.Utils;

public class HistoryMemento : ReactiveObject
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
