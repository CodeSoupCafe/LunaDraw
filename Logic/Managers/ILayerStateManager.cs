using System.Collections.ObjectModel;
using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Managers
{
    public interface ILayerStateManager
    {
        ObservableCollection<Layer> Layers { get; }
        Layer? CurrentLayer { get; set; }
        HistoryManager HistoryManager { get; }
        void AddLayer();
        void RemoveLayer(Layer layer);
        void MoveLayerForward(Layer layer);
        void MoveLayerBackward(Layer layer);
        void MoveLayer(int oldIndex, int newIndex);
        void MoveElementsToLayer(IEnumerable<IDrawableElement> elements, Layer targetLayer);
        void SaveState();
    }
}
