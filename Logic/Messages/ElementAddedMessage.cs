using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Messages
{
    /// <summary>
    /// Message sent when a new element is added to a layer.
    /// </summary>
    public class ElementAddedMessage(IDrawableElement element, Layer targetLayer)
    {
        public IDrawableElement Element { get; } = element;
        public Layer TargetLayer { get; } = targetLayer;
    }
}
