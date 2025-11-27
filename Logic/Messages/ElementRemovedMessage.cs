using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Messages
{
    /// <summary>
    /// Message sent when an element is removed from a layer.
    /// </summary>
    public class ElementRemovedMessage
    {
        public IDrawableElement Element { get; }
        public Layer SourceLayer { get; }

        public ElementRemovedMessage(IDrawableElement element, Layer sourceLayer)
        {
            Element = element;
            SourceLayer = sourceLayer;
        }
    }
}
