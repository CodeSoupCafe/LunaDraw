using ReactiveUI;
using System.Collections.ObjectModel;

namespace LunaDraw.Logic.Models
{
    /// <summary>
    /// Represents a layer in the drawing, containing a collection of drawable elements.
    /// </summary>
    public class Layer : ReactiveObject
    {
        private string _name = "Layer";
        private bool _isVisible = true;
        private bool _isLocked = false;

        public Guid Id { get; } = Guid.NewGuid();

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public ObservableCollection<IDrawableElement> Elements { get; } = new ObservableCollection<IDrawableElement>();

        public bool IsVisible
        {
            get => _isVisible;
            set => this.RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public bool IsLocked
        {
            get => _isLocked;
            set => this.RaiseAndSetIfChanged(ref _isLocked, value);
        }
    }
}
