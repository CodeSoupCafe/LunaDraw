using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Messages
{
    /// <summary>
    /// Message sent when the selection of elements changes.
    /// </summary>
    public class SelectionChangedMessage
    {
        public IEnumerable<IDrawableElement> SelectedElements { get; }

        public SelectionChangedMessage(IEnumerable<IDrawableElement> selectedElements)
        {
            SelectedElements = selectedElements;
        }
    }
}
