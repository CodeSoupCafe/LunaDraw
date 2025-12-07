using System.Reactive;
using LunaDraw.Logic.Managers;
using ReactiveUI;
using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.ViewModels
{
    public class HistoryViewModel : ReactiveObject
    {
        private readonly HistoryManager historyManager;
        private readonly ILayerStateManager layerStateManager;
        private readonly IMessageBus messageBus;

        public HistoryViewModel(ILayerStateManager layerStateManager, IMessageBus messageBus)
        {
            this.layerStateManager = layerStateManager;
            historyManager = layerStateManager.HistoryManager;
            this.messageBus = messageBus;

            // Observables for CanUndo/CanRedo
            var canUndo = this.WhenAnyValue(x => x.historyManager.CanUndo);
            var canRedo = this.WhenAnyValue(x => x.historyManager.CanRedo);

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
            var state = historyManager.Undo();
            if (state != null)
            {
                RestoreState(state);
            }
        }

        private void Redo()
        {
            var state = historyManager.Redo();
            if (state != null)
            {
                RestoreState(state);
            }
        }

        private void RestoreState(List<Layer> state)
        {
            layerStateManager.Layers.Clear();
            foreach (var layer in state)
            {
                layerStateManager.Layers.Add(layer);
            }

            var currentLayerId = layerStateManager.CurrentLayer?.Id;
            layerStateManager.CurrentLayer = layerStateManager.Layers.FirstOrDefault(l => l.Id == currentLayerId) ?? layerStateManager.Layers.FirstOrDefault();

            messageBus.SendMessage(new LunaDraw.Logic.Messages.CanvasInvalidateMessage());
        }
    }
}
