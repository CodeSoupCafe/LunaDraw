using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SkiaSharp;

namespace LunaDraw.Logic.Models
{
    public class NavigationModel : ReactiveObject
    {
        private SKMatrix _totalMatrix;

        public NavigationModel()
        {
            _totalMatrix = SKMatrix.CreateIdentity();
        }

        public SKMatrix TotalMatrix
        {
            get => _totalMatrix;
            set
            {
                // Check if the matrix is very close to identity
                // This helps mitigate floating point inaccuracies over many transformations
                const float epsilon = 1e-6f; // A small value to check for near-zero/near-one
                if (Math.Abs(value.ScaleX - 1) < epsilon &&
                    Math.Abs(value.ScaleY - 1) < epsilon &&
                    Math.Abs(value.SkewX) < epsilon &&
                    Math.Abs(value.SkewY) < epsilon &&
                    Math.Abs(value.TransX) < epsilon &&
                    Math.Abs(value.TransY) < epsilon)
                {
                    value = SKMatrix.CreateIdentity();
                }

                if (!EqualityComparer<SKMatrix>.Default.Equals(_totalMatrix, value))
                {
                    this.RaiseAndSetIfChanged(ref _totalMatrix, value);
                }
            }
        }

        public void Reset()
        {
            TotalMatrix = SKMatrix.CreateIdentity();
        }
    }
}
