using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Messages
{
    /// <summary>
    /// Message sent when a new element is added to a layer.
    /// </summary>
    public class ElementAddedMessage
    {
        public IDrawableElement Element { get; }
        public Layer TargetLayer { get; }

        public ElementAddedMessage(IDrawableElement element, Layer targetLayer)
        {
            Element = element;
            TargetLayer = targetLayer;
        }
    }
}
