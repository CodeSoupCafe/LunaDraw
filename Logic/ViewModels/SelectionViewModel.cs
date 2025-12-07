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
        private readonly SelectionManager _selectionManager;
        private readonly ILayerStateManager _layerStateManager;
        private readonly ClipboardManager _clipboardManager;
        private readonly IMessageBus _messageBus;

        public SelectionViewModel(
            SelectionManager selectionManager, 
            ILayerStateManager layerStateManager,
            ClipboardManager clipboardManager,
            IMessageBus messageBus)
        {
            _selectionManager = selectionManager;
            _layerStateManager = layerStateManager;
            _clipboardManager = clipboardManager;
            _messageBus = messageBus;

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

            canPaste = this.WhenAnyValue(x => x._clipboardManager.HasItems)
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
        }
        
        public ReadOnlyObservableCollection<IDrawableElement> SelectedElements => _selectionManager.Selected;

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

        private void MoveSelectionToLayer(Layer targetLayer)
        {
            if (targetLayer == null || !SelectedElements.Any()) return;
            _layerStateManager.MoveElementsToLayer(SelectedElements, targetLayer);
        }

        private void DeleteSelected() 
        {
             var currentLayer = _layerStateManager.CurrentLayer;
             if (currentLayer is null || !SelectedElements.Any()) return;

            var elementsToRemove = SelectedElements.ToList();
            foreach (var element in elementsToRemove)
            {
              currentLayer.Elements.Remove(element);
            }
            _selectionManager.Clear();
            _messageBus.SendMessage(new CanvasInvalidateMessage());
            _layerStateManager.SaveState();
        }

        private void GroupSelected()
        {
            var currentLayer = _layerStateManager.CurrentLayer;
            if (currentLayer is null || !SelectedElements.Any()) return;

            var elementsToGroup = SelectedElements.ToList();
            var group = new DrawableGroup();

            foreach (var element in elementsToGroup)
            {
              currentLayer.Elements.Remove(element);
              group.Children.Add(element);
            }
            currentLayer.Elements.Add(group);
            _selectionManager.Clear();
            _selectionManager.Add(group);
            _messageBus.SendMessage(new CanvasInvalidateMessage());
            _layerStateManager.SaveState();
        }
        
        private void UngroupSelected()
        {
            var currentLayer = _layerStateManager.CurrentLayer;
            if (currentLayer is null) return;
            var group = SelectedElements.FirstOrDefault() as DrawableGroup;
            if (group != null)
            {
              currentLayer.Elements.Remove(group);
              foreach (var child in group.Children)
              {
                currentLayer.Elements.Add(child);
              }
              _selectionManager.Clear();
              _messageBus.SendMessage(new CanvasInvalidateMessage());
              _layerStateManager.SaveState();
            }
        }

        private void Copy()
        {
            _clipboardManager.Copy(SelectedElements);
        }

        private void Cut()
        {
             var currentLayer = _layerStateManager.CurrentLayer;
            if (currentLayer is null || !SelectedElements.Any()) return;
            _clipboardManager.Copy(SelectedElements);
            
            var elementsToRemove = SelectedElements.ToList();
            foreach (var element in elementsToRemove)
            {
              currentLayer.Elements.Remove(element);
            }
            _selectionManager.Clear();
            _messageBus.SendMessage(new CanvasInvalidateMessage());
            _layerStateManager.SaveState();
        }

        private void Paste()
        {
             var currentLayer = _layerStateManager.CurrentLayer;
            if (currentLayer is null || !_clipboardManager.HasItems) return;
            foreach (var element in _clipboardManager.Paste())
            {
              element.Translate(new SKPoint(10, 10)); // Offset pasted element
              currentLayer.Elements.Add(element);
            }
            _messageBus.SendMessage(new CanvasInvalidateMessage());
            _layerStateManager.SaveState();
        }

        private void SendBackward()
        {
            var currentLayer = _layerStateManager.CurrentLayer;
             if (currentLayer == null || !SelectedElements.Any()) return;
          
          var selected = SelectedElements.First();
          var sortedElements = currentLayer.Elements.OrderBy(e => e.ZIndex).ToList();
          int index = sortedElements.IndexOf(selected);

          if (index > 0)
          {
              var elementBelow = sortedElements[index - 1];
              sortedElements[index - 1] = selected;
              sortedElements[index] = elementBelow;
              ReassignZIndices(sortedElements);
              _messageBus.SendMessage(new CanvasInvalidateMessage());
              _layerStateManager.SaveState();
          }
        }

        private void BringForward()
        {
          var currentLayer = _layerStateManager.CurrentLayer;
          if (currentLayer == null || !SelectedElements.Any()) return;
          
          var selected = SelectedElements.First();
          var sortedElements = currentLayer.Elements.OrderBy(e => e.ZIndex).ToList();
          int index = sortedElements.IndexOf(selected);

          if (index < sortedElements.Count - 1)
          {
              var elementAbove = sortedElements[index + 1];
              sortedElements[index + 1] = selected;
              sortedElements[index] = elementAbove;
              ReassignZIndices(sortedElements);
              _messageBus.SendMessage(new CanvasInvalidateMessage());
              _layerStateManager.SaveState();
          }
        }

        private void SendElementToBack()
        {
          var currentLayer = _layerStateManager.CurrentLayer;
          if (currentLayer == null || !SelectedElements.Any()) return;

          var selected = SelectedElements.First();
          var elements = currentLayer.Elements.ToList();
          
          if (elements.Remove(selected))
          {
              elements.Insert(0, selected);
              // Clear and re-add to trigger ObservableCollection updates properly 
              // or just updating properties if Elements wasn't an ObservableCollection?
              // It is ObservableCollection.
              // Simpler:
              currentLayer.Elements.Clear();
              foreach(var el in elements) currentLayer.Elements.Add(el);

              ReassignZIndices(elements);
              _messageBus.SendMessage(new CanvasInvalidateMessage());
              _layerStateManager.SaveState();
          }
        }

        private void BringElementToFront()
        {
          var currentLayer = _layerStateManager.CurrentLayer;
          if (currentLayer == null || !SelectedElements.Any()) return;

          var selected = SelectedElements.First();
          var elements = currentLayer.Elements.ToList();

          if (elements.Remove(selected))
          {
              elements.Add(selected);
              
              currentLayer.Elements.Clear();
              foreach(var el in elements) currentLayer.Elements.Add(el);

              ReassignZIndices(elements);
              _messageBus.SendMessage(new CanvasInvalidateMessage());
              _layerStateManager.SaveState();
          }
        }
        
        private void ReassignZIndices(IList<IDrawableElement> elements)
        {
              for (int i = 0; i < elements.Count; i++)
              {
                  elements[i].ZIndex = i;
              }
        }
    }
}
