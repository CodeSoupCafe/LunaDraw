using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SkiaSharp;

namespace LunaDraw.Logic.Models
{
    public class NavigationModel : ReactiveObject
    {
        public NavigationModel()
        {
            TotalMatrix = SKMatrix.CreateIdentity();
        }

        [Reactive] public SKMatrix TotalMatrix { get; set; }

        public void Reset()
        {
            TotalMatrix = SKMatrix.CreateIdentity();
        }
    }
}
