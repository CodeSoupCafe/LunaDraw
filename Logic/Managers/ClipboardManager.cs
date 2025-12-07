using LunaDraw.Logic.Models;
using ReactiveUI;

namespace LunaDraw.Logic.Managers
{
    public class ClipboardManager : ReactiveObject
    {
        private List<IDrawableElement> _clipboard = new();

        public void Copy(IEnumerable<IDrawableElement> elements)
        {
            _clipboard = elements.Select(e => e.Clone()).ToList();
            this.RaisePropertyChanged(nameof(HasItems));
        }

        public IEnumerable<IDrawableElement> Paste()
        {
            return _clipboard.Select(e => e.Clone());
        }
        
        public bool HasItems => _clipboard.Count > 0;
    }
}
