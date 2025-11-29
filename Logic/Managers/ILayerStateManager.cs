using System.Collections.ObjectModel;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Managers;

namespace LunaDraw.Logic.Services
{
    public interface ILayerStateManager
    {
        ObservableCollection<Layer> Layers { get; }
        Layer? CurrentLayer { get; set; }
        HistoryManager HistoryManager { get; }
        void AddLayer();
        void RemoveLayer(Layer layer);
        void SaveState();
    }
}
