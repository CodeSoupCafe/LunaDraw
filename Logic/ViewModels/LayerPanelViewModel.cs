using System.Collections.ObjectModel;
using System.Reactive;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using ReactiveUI;

namespace LunaDraw.Logic.ViewModels
{
    public class LayerPanelViewModel : ReactiveObject
    {
        private readonly ILayerStateManager _layerStateManager;
        private readonly IMessageBus _messageBus;

        public LayerPanelViewModel(ILayerStateManager layerStateManager, IMessageBus messageBus)
        {
            _layerStateManager = layerStateManager;
            _messageBus = messageBus;

            _layerStateManager.WhenAnyValue(x => x.CurrentLayer)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(CurrentLayer)));

            // Commands
            AddLayerCommand = ReactiveCommand.Create(() =>
            {
                _layerStateManager.AddLayer();
            }, outputScheduler: RxApp.MainThreadScheduler);

            RemoveLayerCommand = ReactiveCommand.Create<Layer>(layer =>
            {
                _layerStateManager.RemoveLayer(layer);
            }, outputScheduler: RxApp.MainThreadScheduler);

            MoveLayerForwardCommand = ReactiveCommand.Create<Layer>(layer =>
            {
                _layerStateManager.MoveLayerForward(layer);
            }, outputScheduler: RxApp.MainThreadScheduler);

            MoveLayerBackwardCommand = ReactiveCommand.Create<Layer>(layer =>
            {
                _layerStateManager.MoveLayerBackward(layer);
            }, outputScheduler: RxApp.MainThreadScheduler);
            
            ToggleLayerVisibilityCommand = ReactiveCommand.Create<Layer>(layer =>
            {
                if (layer != null)
                {
                    layer.IsVisible = !layer.IsVisible;
                    _messageBus.SendMessage(new CanvasInvalidateMessage());
                }
            }, outputScheduler: RxApp.MainThreadScheduler);

            ToggleLayerLockCommand = ReactiveCommand.Create<Layer>(layer =>
            {
                if (layer != null)
                {
                    layer.IsLocked = !layer.IsLocked;
                }
            }, outputScheduler: RxApp.MainThreadScheduler);
        }

        public ObservableCollection<Layer> Layers => _layerStateManager.Layers;

        public Layer? CurrentLayer
        {
            get => _layerStateManager.CurrentLayer;
            set => _layerStateManager.CurrentLayer = value;
        }

        public ReactiveCommand<Unit, Unit> AddLayerCommand { get; }
        public ReactiveCommand<Layer, Unit> RemoveLayerCommand { get; }
        public ReactiveCommand<Layer, Unit> MoveLayerForwardCommand { get; }
        public ReactiveCommand<Layer, Unit> MoveLayerBackwardCommand { get; }
        public ReactiveCommand<Layer, Unit> ToggleLayerVisibilityCommand { get; }
        public ReactiveCommand<Layer, Unit> ToggleLayerLockCommand { get; }
    }
}
