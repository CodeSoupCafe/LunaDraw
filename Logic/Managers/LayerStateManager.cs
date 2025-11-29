using System.Collections.ObjectModel;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using ReactiveUI;

namespace LunaDraw.Logic.Services
{
  public class LayerStateManager : ReactiveObject, ILayerStateManager
  {
    public ObservableCollection<Layer> Layers { get; } = [];
    public HistoryManager HistoryManager { get; } = new HistoryManager();

    private Layer? _currentLayer;
    public Layer? CurrentLayer
    {
      get => _currentLayer;
      set => this.RaiseAndSetIfChanged(ref _currentLayer, value);
    }

    private readonly IMessageBus _messageBus;

    public LayerStateManager(IMessageBus messageBus)
    {
      _messageBus = messageBus;
      // Initialize with a default layer
      var initialLayer = new Layer { Name = "Layer 1" };
      Layers.Add(initialLayer);
      CurrentLayer = initialLayer;

      _messageBus.Listen<DrawingStateChangedMessage>().Subscribe(_ => SaveState());

      SaveState();
    }

    public void AddLayer()
    {
      var newLayer = new Layer { Name = $"Layer {Layers.Count + 1}" };
      Layers.Add(newLayer);
      CurrentLayer = newLayer;
      SaveState();
    }

    public void RemoveLayer(Layer layer)
    {
      if (Layers.Count > 1)
      {
        Layers.Remove(layer);
        CurrentLayer = Layers.First();
        SaveState();
      }
    }

    public void SaveState()
    {
      HistoryManager.SaveState(Layers);
    }
  }
}
