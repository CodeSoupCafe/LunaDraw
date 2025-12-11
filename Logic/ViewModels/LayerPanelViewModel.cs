using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using ReactiveUI;

namespace LunaDraw.Logic.ViewModels
{
    public class LayerPanelViewModel : ReactiveObject
    {
        private readonly ILayerStateManager layerStateManager;
        private readonly IMessageBus messageBus;

        public LayerPanelViewModel(ILayerStateManager layerStateManager, IMessageBus messageBus)
        {
            this.layerStateManager = layerStateManager;
            this.messageBus = messageBus;

            layerStateManager.WhenAnyValue(x => x.CurrentLayer)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(CurrentLayer)));

            // Commands
            AddLayerCommand = ReactiveCommand.Create(() =>
            {
                layerStateManager.AddLayer();
            }, outputScheduler: RxApp.MainThreadScheduler);

            var layersChanged = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => layerStateManager.Layers.CollectionChanged += h,
                h => layerStateManager.Layers.CollectionChanged -= h)
                .Select(_ => Unit.Default)
                .StartWith(Unit.Default);

            var currentLayerChanged = layerStateManager.WhenAnyValue(x => x.CurrentLayer)
                .Select(_ => Unit.Default);

            var canRemoveLayer = Observable.Merge(layersChanged, currentLayerChanged)
                .Select(_ => layerStateManager.CurrentLayer != null && layerStateManager.Layers.Count > 1)
                .ObserveOn(RxApp.MainThreadScheduler);

            RemoveLayerCommand = ReactiveCommand.Create(() =>
            {
                if (layerStateManager.CurrentLayer != null)
                {
                    layerStateManager.RemoveLayer(layerStateManager.CurrentLayer);
                }
            }, 
            canExecute: canRemoveLayer, 
            outputScheduler: RxApp.MainThreadScheduler);

            MoveLayerForwardCommand = ReactiveCommand.Create<Layer>(layer =>
            {
                layerStateManager.MoveLayerForward(layer);
            }, outputScheduler: RxApp.MainThreadScheduler);

            MoveLayerBackwardCommand = ReactiveCommand.Create<Layer>(layer =>
            {
                layerStateManager.MoveLayerBackward(layer);
            }, outputScheduler: RxApp.MainThreadScheduler);

            ToggleLayerVisibilityCommand = ReactiveCommand.Create<Layer>(layer =>
            {
                if (layer != null)
                {
                    layer.IsVisible = !layer.IsVisible;
                    messageBus.SendMessage(new CanvasInvalidateMessage());
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

        public ObservableCollection<Layer> Layers => layerStateManager.Layers;

        public Layer? CurrentLayer
        {
            get => layerStateManager.CurrentLayer;
            set => layerStateManager.CurrentLayer = value;
        }

        public ReactiveCommand<Unit, Unit> AddLayerCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveLayerCommand { get; }
        public ReactiveCommand<Layer, Unit> MoveLayerForwardCommand { get; }
        public ReactiveCommand<Layer, Unit> MoveLayerBackwardCommand { get; }
        public ReactiveCommand<Layer, Unit> ToggleLayerVisibilityCommand { get; }
        public ReactiveCommand<Layer, Unit> ToggleLayerLockCommand { get; }
    }
}
