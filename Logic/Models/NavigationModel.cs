using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Models
{
    public class NavigationModel : ReactiveObject
    {
        private SKMatrix viewMatrix = SKMatrix.CreateIdentity();

        // Single source of truth - this is what gets applied to the canvas
        public SKMatrix ViewMatrix
        {
            get => viewMatrix;
            set => this.RaiseAndSetIfChanged(ref viewMatrix, value);
        }

        private float canvasWidth;
        public float CanvasWidth
        {
            get => canvasWidth;
            set => this.RaiseAndSetIfChanged(ref canvasWidth, value);
        }

        private float canvasHeight;
        public float CanvasHeight
        {
            get => canvasHeight;
            set => this.RaiseAndSetIfChanged(ref canvasHeight, value);
        }

        public void Reset()
        {
            ViewMatrix = SKMatrix.CreateIdentity();
        }
    }
}