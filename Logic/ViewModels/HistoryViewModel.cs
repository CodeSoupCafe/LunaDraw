using System.Reactive;
using LunaDraw.Logic.Managers;
using ReactiveUI;
using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.ViewModels
{
    public class HistoryViewModel : ReactiveObject
    {
        private readonly HistoryManager _historyManager;
        private readonly ILayerStateManager _layerStateManager;
        private readonly IMessageBus _messageBus;

        public HistoryViewModel(ILayerStateManager layerStateManager, IMessageBus messageBus)
        {
            _layerStateManager = layerStateManager;
            _historyManager = layerStateManager.HistoryManager;
            _messageBus = messageBus;

            // Observables for CanUndo/CanRedo
            var canUndo = this.WhenAnyValue(x => x._historyManager.CanUndo);
            var canRedo = this.WhenAnyValue(x => x._historyManager.CanRedo);

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
            var state = _historyManager.Undo();
            if (state != null)
            {
                RestoreState(state);
            }
        }

        private void Redo()
        {
            var state = _historyManager.Redo();
            if (state != null)
            {
                RestoreState(state);
            }
        }

        private void RestoreState(List<Layer> state)
        {
             _layerStateManager.Layers.Clear();
             foreach (var layer in state)
             {
                 _layerStateManager.Layers.Add(layer);
             }
             
             var currentLayerId = _layerStateManager.CurrentLayer?.Id;
             _layerStateManager.CurrentLayer = _layerStateManager.Layers.FirstOrDefault(l => l.Id == currentLayerId) ?? _layerStateManager.Layers.FirstOrDefault();
             
             _messageBus.SendMessage(new LunaDraw.Logic.Messages.CanvasInvalidateMessage());
        }
    }
}
