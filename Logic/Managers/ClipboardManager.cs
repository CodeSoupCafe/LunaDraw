using LunaDraw.Logic.Models;
using ReactiveUI;

namespace LunaDraw.Logic.Managers
{
    public class ClipboardManager : ReactiveObject
    {
        private List<IDrawableElement> clipboard = new();

        public void Copy(IEnumerable<IDrawableElement> elements)
        {
            clipboard = elements.Select(e => e.Clone()).ToList();
            this.RaisePropertyChanged(nameof(HasItems));
        }

        public IEnumerable<IDrawableElement> Paste()
        {
            return clipboard.Select(e => e.Clone());
        }

        public bool HasItems => clipboard.Count > 0;
    }
}
