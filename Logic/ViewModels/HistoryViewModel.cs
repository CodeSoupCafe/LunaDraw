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

using System.Reactive;
using LunaDraw.Logic.Utils;
using ReactiveUI;
using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.ViewModels;

public class HistoryViewModel : ReactiveObject
{
  private readonly HistoryMemento historyMemento;
  private readonly ILayerFacade layerFacade;
  private readonly IMessageBus messageBus;

  public HistoryViewModel(ILayerFacade layerFacade, IMessageBus messageBus)
  {
    this.layerFacade = layerFacade;
    historyMemento = layerFacade.HistoryMemento;
    this.messageBus = messageBus;

    // Observables for CanUndo/CanRedo
    var canUndo = this.WhenAnyValue(x => x.historyMemento.CanUndo);
    var canRedo = this.WhenAnyValue(x => x.historyMemento.CanRedo);

    UndoCommand = ReactiveCommand.Create(Undo, canUndo, RxApp.MainThreadScheduler);
    RedoCommand = ReactiveCommand.Create(Redo, canRedo, RxApp.MainThreadScheduler);

    // Expose properties for binding
    canUndoProp = canUndo.ToProperty(this, x => x.CanUndo);
    canRedoProp = canRedo.ToProperty(this, x => x.CanRedo);
  }

  private readonly ObservableAsPropertyHelper<bool> canUndoProp;
  public bool CanUndo => canUndoProp.Value;

  private readonly ObservableAsPropertyHelper<bool> canRedoProp;
  public bool CanRedo => canRedoProp.Value;

  public ReactiveCommand<Unit, Unit> UndoCommand { get; }
  public ReactiveCommand<Unit, Unit> RedoCommand { get; }

  private void Undo()
  {
    var state = historyMemento.Undo();
    if (state != null)
    {
      RestoreState(state);
    }
  }

  private void Redo()
  {
    var state = historyMemento.Redo();
    if (state != null)
    {
      RestoreState(state);
    }
  }

  private void RestoreState(List<Layer> state)
  {
    layerFacade.Layers.Clear();
    foreach (var layer in state)
    {
      layerFacade.Layers.Add(layer);
    }

    var currentLayerId = layerFacade.CurrentLayer?.Id;
    layerFacade.CurrentLayer = layerFacade.Layers.FirstOrDefault(l => l.Id == currentLayerId) ?? layerFacade.Layers.FirstOrDefault();

    messageBus.SendMessage(new LunaDraw.Logic.Messages.CanvasInvalidateMessage());
  }
}
