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

            // Calculate Centroids
            SKPoint oldCentroid = new SKPoint((prevPoint.X + pivotPoint.X) / 2, (prevPoint.Y + pivotPoint.Y) / 2);
            SKPoint newCentroid = new SKPoint((newPoint.X + pivotPoint.X) / 2, (newPoint.Y + pivotPoint.Y) / 2);

            // 1. Translation (Move from Old Centroid to New Centroid)
            SKPoint translation = newCentroid - oldCentroid;
            touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(translation.X, translation.Y));

            // Calculate Vectors for Rotation/Scale
            SKPoint oldVector = prevPoint - pivotPoint;
            SKPoint newVector = newPoint - pivotPoint;

            // 2. Rotation
            float oldAngle = (float)Math.Atan2(oldVector.Y, oldVector.X);
            float newAngle = (float)Math.Atan2(newVector.Y, newVector.X);
            float angleDiff = newAngle - oldAngle;
            float degrees = angleDiff * 180f / (float)Math.PI;
            returnAngle = degrees;

            // Rotate around the NEW Centroid
            touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateRotationDegrees(degrees, newCentroid.X, newCentroid.Y));

            // 3. Scale
            float oldDist = Magnitude(oldVector);
            float newDist = Magnitude(newVector);
            
            float scaleX = 1;
            if (oldDist > 0.001f) 
            {
                scaleX = newDist / oldDist;
            }
            
            // Scale around the NEW Centroid
            touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateScale(scaleX, scaleX, newCentroid.X, newCentroid.Y));

            return touchMatrix;
        }

        private static float Magnitude(SKPoint point)
        {
            return (float)Math.Sqrt(Math.Pow(point.X, 2) + Math.Pow(point.Y, 2));
        }
    }
}