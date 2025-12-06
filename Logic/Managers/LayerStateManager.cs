using System.Collections.ObjectModel;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using ReactiveUI;

namespace LunaDraw.Logic.Managers
{
  public class LayerStateManager : ReactiveObject, ILayerStateManager
  {
    public ObservableCollection<Layer> Layers { get; } = [];
    public HistoryManager HistoryManager { get; } = new HistoryManager();

    private Layer? currentLayer;
    public Layer? CurrentLayer
    {
      get => currentLayer;
      set => this.RaiseAndSetIfChanged(ref currentLayer, value);
    }

    private readonly IMessageBus messageBus;

    public LayerStateManager(IMessageBus messageBus)
    {
      this.messageBus = messageBus;
      // Initialize with a default layer
      var initialLayer = new Layer { Name = "Layer 1" };
      Layers.Add(initialLayer);
      CurrentLayer = initialLayer;

      this.messageBus.Listen<DrawingStateChangedMessage>().Subscribe(_ => SaveState());

      SaveState();
    }

    public void AddLayer()
    {
      var newLayer = new Layer { Name = $"Layer {Layers.Count + 1}" };
      Layers.Add(newLayer);
      CurrentLayer = newLayer;
      SaveState();
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void RemoveLayer(Layer layer)
    {
      if (Layers.Count > 1)
      {
        Layers.Remove(layer);
        CurrentLayer = Layers.First();
        SaveState();
        messageBus.SendMessage(new CanvasInvalidateMessage());
      }
    }

    public void MoveLayerForward(Layer layer)
    {
      int index = Layers.IndexOf(layer);
      if (index >= 0 && index < Layers.Count - 1)
      {
        Layers.Move(index, index + 1);
        SaveState();
        messageBus.SendMessage(new CanvasInvalidateMessage());
      }
    }

    public void MoveLayerBackward(Layer layer)
    {
      int index = Layers.IndexOf(layer);
      if (index > 0)
      {
        Layers.Move(index, index - 1);
        SaveState();
        messageBus.SendMessage(new CanvasInvalidateMessage());
      }
    }

    public void MoveLayer(int oldIndex, int newIndex)
    {
        if (oldIndex >= 0 && oldIndex < Layers.Count && newIndex >= 0 && newIndex < Layers.Count)
        {
            Layers.Move(oldIndex, newIndex);
            SaveState();
            messageBus.SendMessage(new CanvasInvalidateMessage());
        }
    }

    public void MoveElementsToLayer(IEnumerable<IDrawableElement> elements, Layer targetLayer)
    {
        if (!Layers.Contains(targetLayer)) return;

        bool changed = false;
        foreach (var element in elements.ToList()) // ToList to avoid modification during enumeration
        {
            // Find the layer containing this element
            var sourceLayer = Layers.FirstOrDefault(l => l.Elements.Contains(element));
            if (sourceLayer != null && sourceLayer != targetLayer)
            {
                sourceLayer.Elements.Remove(element);
                targetLayer.Elements.Add(element);
                changed = true;
            }
        }

        if (changed)
        {
            SaveState();
            messageBus.SendMessage(new CanvasInvalidateMessage());
        }
    }

    public void SaveState()
    {
      HistoryManager.SaveState(Layers);
    }
  }
}