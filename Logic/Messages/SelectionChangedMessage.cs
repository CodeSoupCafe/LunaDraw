using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Messages
{
    /// <summary>
    /// Message sent when the selection of elements changes.
    /// </summary>
    public class SelectionChangedMessage(IEnumerable<IDrawableElement> selectedElements)
    {
        public IEnumerable<IDrawableElement> SelectedElements { get; } = selectedElements;
    }
}
