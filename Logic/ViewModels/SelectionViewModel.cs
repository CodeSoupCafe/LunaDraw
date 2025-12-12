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
using System.Reactive;
using System.Reactive.Linq;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.ViewModels
{
  public class SelectionViewModel : ReactiveObject
  {
    private readonly SelectionObserver selectionObserver;
    private readonly ILayerFacade layerFacade;
    private readonly ClipboardMemento clipboardManager;
    private readonly IMessageBus messageBus;

    public SelectionViewModel(
        SelectionObserver selectionObserver,
        ILayerFacade layerFacade,
        ClipboardMemento clipboardManager,
        IMessageBus messageBus)
    {
      this.selectionObserver = selectionObserver;
      this.layerFacade = layerFacade;
      this.clipboardManager = clipboardManager;
      this.messageBus = messageBus;

      // OAPHs
      var hasSelection = this.WhenAnyValue(x => x.SelectedElements.Count)
          .Select(count => count > 0);

      canDelete = hasSelection.ToProperty(this, x => x.CanDelete);

      canGroup = this.WhenAnyValue(x => x.SelectedElements.Count)
          .Select(count => count > 1)
          .ToProperty(this, x => x.CanGroup);

      canUngroup = this.WhenAnyValue(x => x.SelectedElements.Count)
          .Select(count => count == 1 && SelectedElements.FirstOrDefault() is DrawableGroup)
          .ToProperty(this, x => x.CanUngroup);

      canPaste = this.WhenAnyValue(x => x.clipboardManager.HasItems)
          .ToProperty(this, x => x.CanPaste);

      // Commands
      DeleteSelectedCommand = ReactiveCommand.Create(DeleteSelected, hasSelection, RxApp.MainThreadScheduler);
      GroupSelectedCommand = ReactiveCommand.Create(GroupSelected, this.WhenAnyValue(x => x.CanGroup), RxApp.MainThreadScheduler);
      UngroupSelectedCommand = ReactiveCommand.Create(UngroupSelected, this.WhenAnyValue(x => x.CanUngroup), RxApp.MainThreadScheduler);
      CopyCommand = ReactiveCommand.Create(Copy, hasSelection, RxApp.MainThreadScheduler);
      CutCommand = ReactiveCommand.Create(Cut, hasSelection, RxApp.MainThreadScheduler);
      PasteCommand = ReactiveCommand.Create(Paste, this.WhenAnyValue(x => x.CanPaste), RxApp.MainThreadScheduler);

      SendBackwardCommand = ReactiveCommand.Create(SendBackward, hasSelection, RxApp.MainThreadScheduler);
      BringForwardCommand = ReactiveCommand.Create(BringForward, hasSelection, RxApp.MainThreadScheduler);
      SendElementToBackCommand = ReactiveCommand.Create(SendElementToBack, hasSelection, RxApp.MainThreadScheduler);
      BringElementToFrontCommand = ReactiveCommand.Create(BringElementToFront, hasSelection, RxApp.MainThreadScheduler);
      MoveSelectionToLayerCommand = ReactiveCommand.Create<Layer>(MoveSelectionToLayer, hasSelection, RxApp.MainThreadScheduler);
      MoveSelectionToNewLayerCommand = ReactiveCommand.Create(MoveSelectionToNewLayer, hasSelection, RxApp.MainThreadScheduler);
    }

    public ReadOnlyObservableCollection<IDrawableElement> SelectedElements => selectionObserver.Selected;

    private readonly ObservableAsPropertyHelper<bool> canDelete;
    public bool CanDelete => canDelete.Value;

    private readonly ObservableAsPropertyHelper<bool> canGroup;
    public bool CanGroup => canGroup.Value;

    private readonly ObservableAsPropertyHelper<bool> canUngroup;
    public bool CanUngroup => canUngroup.Value;

    private readonly ObservableAsPropertyHelper<bool> canPaste;
    public bool CanPaste => canPaste.Value;

    public ReactiveCommand<Unit, Unit> DeleteSelectedCommand { get; }
    public ReactiveCommand<Unit, Unit> GroupSelectedCommand { get; }
    public ReactiveCommand<Unit, Unit> UngroupSelectedCommand { get; }
    public ReactiveCommand<Unit, Unit> CopyCommand { get; }
    public ReactiveCommand<Unit, Unit> CutCommand { get; }
    public ReactiveCommand<Unit, Unit> PasteCommand { get; }
    public ReactiveCommand<Unit, Unit> SendBackwardCommand { get; }
    public ReactiveCommand<Unit, Unit> BringForwardCommand { get; }
    public ReactiveCommand<Unit, Unit> SendElementToBackCommand { get; }
    public ReactiveCommand<Unit, Unit> BringElementToFrontCommand { get; }
    public ReactiveCommand<Layer, Unit> MoveSelectionToLayerCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveSelectionToNewLayerCommand { get; }

    private void MoveSelectionToNewLayer()
    {
      if (!SelectedElements.Any()) return;

      layerFacade.AddLayer();
      var newLayer = layerFacade.CurrentLayer;

      if (newLayer != null)
      {
        layerFacade.MoveElementsToLayer(SelectedElements, newLayer);
      }
    }

    private void MoveSelectionToLayer(Layer targetLayer)
    {
      if (targetLayer == null || !SelectedElements.Any()) return;
      layerFacade.MoveElementsToLayer(SelectedElements, targetLayer);
    }

    private void DeleteSelected()
    {
      var currentLayer = layerFacade.CurrentLayer;
      if (currentLayer is null || !SelectedElements.Any()) return;

      var elementsToRemove = SelectedElements.ToList();
      foreach (var element in elementsToRemove)
      {
        currentLayer.Elements.Remove(element);
      }
      selectionObserver.Clear();
      messageBus.SendMessage(new CanvasInvalidateMessage());
      layerFacade.SaveState();
    }

    private void GroupSelected()
    {
      var currentLayer = layerFacade.CurrentLayer;
      if (currentLayer is null || !SelectedElements.Any()) return;

      var elementsToGroup = SelectedElements.ToList();
      var group = new DrawableGroup();

      foreach (var element in elementsToGroup)
      {
        currentLayer.Elements.Remove(element);
        group.Children.Add(element);
      }
      currentLayer.Elements.Add(group);
      selectionObserver.Clear();
      selectionObserver.Add(group);
      messageBus.SendMessage(new CanvasInvalidateMessage());
      layerFacade.SaveState();
    }

    private void UngroupSelected()
    {
      var currentLayer = layerFacade.CurrentLayer;
      if (currentLayer is null) return;
      var group = SelectedElements.FirstOrDefault() as DrawableGroup;
      if (group != null)
      {
        currentLayer.Elements.Remove(group);
        foreach (var child in group.Children)
        {
          currentLayer.Elements.Add(child);
        }
        selectionObserver.Clear();
        messageBus.SendMessage(new CanvasInvalidateMessage());
        layerFacade.SaveState();
      }
    }

    private void Copy()
    {
      clipboardManager.Copy(SelectedElements);
    }

    private void Cut()
    {
      var currentLayer = layerFacade.CurrentLayer;
      if (currentLayer is null || !SelectedElements.Any()) return;
      clipboardManager.Copy(SelectedElements);

      var elementsToRemove = SelectedElements.ToList();
      foreach (var element in elementsToRemove)
      {
        currentLayer.Elements.Remove(element);
      }
      selectionObserver.Clear();
      messageBus.SendMessage(new CanvasInvalidateMessage());
      layerFacade.SaveState();
    }

    private void Paste()
    {
      var currentLayer = layerFacade.CurrentLayer;
      if (currentLayer is null || !clipboardManager.HasItems) return;
      foreach (var element in clipboardManager.Paste())
      {
        element.Translate(new SKPoint(10, 10)); // Offset pasted element
        currentLayer.Elements.Add(element);
      }
      messageBus.SendMessage(new CanvasInvalidateMessage());
      layerFacade.SaveState();
    }

    private void SendBackward()
    {
      var currentLayer = layerFacade.CurrentLayer;
      if (currentLayer == null || !SelectedElements.Any()) return;

      var selected = SelectedElements.First();
      var index = currentLayer.Elements.IndexOf(selected);

      if (index > 0)
      {
        currentLayer.Elements.Move(index, index - 1);
        ReassignZIndices(currentLayer.Elements);
        messageBus.SendMessage(new CanvasInvalidateMessage());
        layerFacade.SaveState();
      }
    }

    private void BringForward()
    {
      var currentLayer = layerFacade.CurrentLayer;
      if (currentLayer == null || !SelectedElements.Any()) return;

      var selected = SelectedElements.First();
      var index = currentLayer.Elements.IndexOf(selected);

      if (index < currentLayer.Elements.Count - 1)
      {
        currentLayer.Elements.Move(index, index + 1);
        ReassignZIndices(currentLayer.Elements);
        messageBus.SendMessage(new CanvasInvalidateMessage());
        layerFacade.SaveState();
      }
    }

    private void SendElementToBack()
    {
      var currentLayer = layerFacade.CurrentLayer;
      if (currentLayer == null || !SelectedElements.Any()) return;

      var selected = SelectedElements.First();
      var index = currentLayer.Elements.IndexOf(selected);

      if (index > 0)
      {
        currentLayer.Elements.Move(index, 0);
        ReassignZIndices(currentLayer.Elements);
        messageBus.SendMessage(new CanvasInvalidateMessage());
        layerFacade.SaveState();
      }
    }

    private void BringElementToFront()
    {
      var currentLayer = layerFacade.CurrentLayer;
      if (currentLayer == null || !SelectedElements.Any()) return;

      var selected = SelectedElements.First();
      var index = currentLayer.Elements.IndexOf(selected);

      if (index < currentLayer.Elements.Count - 1)
      {
        currentLayer.Elements.Move(index, currentLayer.Elements.Count - 1);
        ReassignZIndices(currentLayer.Elements);
        messageBus.SendMessage(new CanvasInvalidateMessage());
        layerFacade.SaveState();
      }
    }

    private static void ReassignZIndices(IList<IDrawableElement> elements)
    {
      for (int i = 0; i < elements.Count; i++)
      {
        elements[i].ZIndex = i;
      }
    }
  }
}
