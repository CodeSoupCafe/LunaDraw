using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Messages
{
    /// <summary>
    /// Message sent when an element is removed from a layer.
    /// </summary>
    public class ElementRemovedMessage(IDrawableElement element, Layer sourceLayer)
    {
        public IDrawableElement Element { get; } = element;
        public Layer SourceLayer { get; } = sourceLayer;
    }
}
