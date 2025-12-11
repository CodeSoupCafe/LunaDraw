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
    private readonly SelectionManager selectionManager;
    private readonly ILayerStateManager layerStateManager;
    private readonly ClipboardManager clipboardManager;
    private readonly IMessageBus messageBus;

    public SelectionViewModel(
        SelectionManager selectionManager,
        ILayerStateManager layerStateManager,
        ClipboardManager clipboardManager,
        IMessageBus messageBus)
    {
      this.selectionManager = selectionManager;
      this.layerStateManager = layerStateManager;
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

    public ReadOnlyObservableCollection<IDrawableElement> SelectedElements => selectionManager.Selected;

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

      layerStateManager.AddLayer();
      var newLayer = layerStateManager.CurrentLayer;

      if (newLayer != null)
      {
        layerStateManager.MoveElementsToLayer(SelectedElements, newLayer);
      }
    }

    private void MoveSelectionToLayer(Layer targetLayer)
    {
      if (targetLayer == null || !SelectedElements.Any()) return;
      layerStateManager.MoveElementsToLayer(SelectedElements, targetLayer);
    }

    private void DeleteSelected()
    {
      var currentLayer = layerStateManager.CurrentLayer;
      if (currentLayer is null || !SelectedElements.Any()) return;

      var elementsToRemove = SelectedElements.ToList();
      foreach (var element in elementsToRemove)
      {
        currentLayer.Elements.Remove(element);
      }
      selectionManager.Clear();
      messageBus.SendMessage(new CanvasInvalidateMessage());
      layerStateManager.SaveState();
    }

    private void GroupSelected()
    {
      var currentLayer = layerStateManager.CurrentLayer;
      if (currentLayer is null || !SelectedElements.Any()) return;

      var elementsToGroup = SelectedElements.ToList();
      var group = new DrawableGroup();

      foreach (var element in elementsToGroup)
      {
        currentLayer.Elements.Remove(element);
        group.Children.Add(element);
      }
      currentLayer.Elements.Add(group);
      selectionManager.Clear();
      selectionManager.Add(group);
      messageBus.SendMessage(new CanvasInvalidateMessage());
      layerStateManager.SaveState();
    }

    private void UngroupSelected()
    {
      var currentLayer = layerStateManager.CurrentLayer;
      if (currentLayer is null) return;
      var group = SelectedElements.FirstOrDefault() as DrawableGroup;
      if (group != null)
      {
        currentLayer.Elements.Remove(group);
        foreach (var child in group.Children)
        {
          currentLayer.Elements.Add(child);
        }
        selectionManager.Clear();
        messageBus.SendMessage(new CanvasInvalidateMessage());
        layerStateManager.SaveState();
      }
    }

    private void Copy()
    {
      clipboardManager.Copy(SelectedElements);
    }

    private void Cut()
    {
      var currentLayer = layerStateManager.CurrentLayer;
      if (currentLayer is null || !SelectedElements.Any()) return;
      clipboardManager.Copy(SelectedElements);

      var elementsToRemove = SelectedElements.ToList();
      foreach (var element in elementsToRemove)
      {
        currentLayer.Elements.Remove(element);
      }
      selectionManager.Clear();
      messageBus.SendMessage(new CanvasInvalidateMessage());
      layerStateManager.SaveState();
    }

    private void Paste()
    {
      var currentLayer = layerStateManager.CurrentLayer;
      if (currentLayer is null || !clipboardManager.HasItems) return;
      foreach (var element in clipboardManager.Paste())
      {
        element.Translate(new SKPoint(10, 10)); // Offset pasted element
        currentLayer.Elements.Add(element);
      }
      messageBus.SendMessage(new CanvasInvalidateMessage());
      layerStateManager.SaveState();
    }

    private void SendBackward()
    {
      var currentLayer = layerStateManager.CurrentLayer;
      if (currentLayer == null || !SelectedElements.Any()) return;

      var selected = SelectedElements.First();
      var index = currentLayer.Elements.IndexOf(selected);

      if (index > 0)
      {
        currentLayer.Elements.Move(index, index - 1);
        ReassignZIndices(currentLayer.Elements);
        messageBus.SendMessage(new CanvasInvalidateMessage());
        layerStateManager.SaveState();
      }
    }

    private void BringForward()
    {
      var currentLayer = layerStateManager.CurrentLayer;
      if (currentLayer == null || !SelectedElements.Any()) return;

      var selected = SelectedElements.First();
      var index = currentLayer.Elements.IndexOf(selected);

      if (index < currentLayer.Elements.Count - 1)
      {
        currentLayer.Elements.Move(index, index + 1);
        ReassignZIndices(currentLayer.Elements);
        messageBus.SendMessage(new CanvasInvalidateMessage());
        layerStateManager.SaveState();
      }
    }

    private void SendElementToBack()
    {
      var currentLayer = layerStateManager.CurrentLayer;
      if (currentLayer == null || !SelectedElements.Any()) return;

      var selected = SelectedElements.First();
      var index = currentLayer.Elements.IndexOf(selected);

      if (index > 0)
      {
        currentLayer.Elements.Move(index, 0);
        ReassignZIndices(currentLayer.Elements);
        messageBus.SendMessage(new CanvasInvalidateMessage());
        layerStateManager.SaveState();
      }
    }

    private void BringElementToFront()
    {
      var currentLayer = layerStateManager.CurrentLayer;
      if (currentLayer == null || !SelectedElements.Any()) return;

      var selected = SelectedElements.First();
      var index = currentLayer.Elements.IndexOf(selected);

      if (index < currentLayer.Elements.Count - 1)
      {
        currentLayer.Elements.Move(index, currentLayer.Elements.Count - 1);
        ReassignZIndices(currentLayer.Elements);
        messageBus.SendMessage(new CanvasInvalidateMessage());
        layerStateManager.SaveState();
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
