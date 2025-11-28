namespace LunaDraw.Logic.Utils
{
    using System;
    using SkiaSharp;

    public class TouchManipulationManager
    {
        public SKMatrix OneFingerManipulate(SKPoint prevPoint, SKPoint newPoint, SKPoint pivotPoint, ref float rotationAngle)
        {
            SKMatrix touchMatrix = SKMatrix.CreateIdentity();

            // Calculate the vector difference
            SKPoint delta = newPoint - prevPoint;

            // Create a Translation Matrix for this difference
            // Note: Using PostConcat to accumulate if there were other transforms, but here it's just one.
            // The doc uses PostConcat(ref touchMatrix, ...). 
            // Since touchMatrix is Identity, PostConcat(Translation) is just Translation.
            touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(delta.X, delta.Y));

            return touchMatrix;
        }

        public SKMatrix TwoFingerManipulate(SKPoint prevPoint, SKPoint newPoint, SKPoint pivotPoint, ref float returnAngle)
        {
            SKMatrix touchMatrix = SKMatrix.CreateIdentity();

            SKPoint oldVector = prevPoint - pivotPoint;
            SKPoint newVector = newPoint - pivotPoint;

            // Calculate rotation
            float oldAngle = (float)Math.Atan2(oldVector.Y, oldVector.X);
            float newAngle = (float)Math.Atan2(newVector.Y, newVector.X);
            float angleDiff = newAngle - oldAngle;

            // Convert to degrees
            float degrees = angleDiff * 180f / (float)Math.PI;
            returnAngle = degrees;

            // Create Rotation Matrix centered at pivotPoint
            touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateRotationDegrees(degrees, pivotPoint.X, pivotPoint.Y));

            // Calculate scale
            // Avoid division by zero
            float oldDist = Magnitude(oldVector);
            float newDist = Magnitude(newVector);
            
            float scaleX = 1;
            if (oldDist > 0.001f) 
            {
                scaleX = newDist / oldDist;
            }
            
            float scaleY = scaleX; // Isotropic scale

            // Create Scale Matrix centered at pivotPoint
            // SKMatrix.CreateScale(sx, sy, pivotX, pivotY)
            touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateScale(scaleX, scaleY, pivotPoint.X, pivotPoint.Y));

            return touchMatrix;
        }

        private static float Magnitude(SKPoint point)
        {
            return (float)Math.Sqrt(Math.Pow(point.X, 2) + Math.Pow(point.Y, 2));
        }
    }
}