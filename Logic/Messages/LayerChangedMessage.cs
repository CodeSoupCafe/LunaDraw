using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Messages
{
    /// <summary>
    /// Message sent when a layer's properties (e.g., visibility, lock status) change.
    /// </summary>
    public class LayerChangedMessage
    {
        public Layer ChangedLayer { get; }

        public LayerChangedMessage(Layer changedLayer)
        {
            ChangedLayer = changedLayer;
        }
    }
}
