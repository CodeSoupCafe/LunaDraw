using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Models
{
    public class NavigationModel : ReactiveObject
    {
        private SKMatrix userMatrix;

        private SKMatrix totalMatrix;

        public NavigationModel()
        {
            userMatrix = SKMatrix.CreateIdentity();
            totalMatrix = SKMatrix.CreateIdentity();
        }

        public SKMatrix UserMatrix
        {
            get => userMatrix;
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

                if (!EqualityComparer<SKMatrix>.Default.Equals(userMatrix, value))
                {
                    this.RaiseAndSetIfChanged(ref userMatrix, value);
                }
            }
        }

        public SKMatrix TotalMatrix
        {
            get => totalMatrix;
            set => this.RaiseAndSetIfChanged(ref totalMatrix, value);
        }

        public void Reset()
        {
            UserMatrix = SKMatrix.CreateIdentity();
            TotalMatrix = SKMatrix.CreateIdentity();
        }
    }
}