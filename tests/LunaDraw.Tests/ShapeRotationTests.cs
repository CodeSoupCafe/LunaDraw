using SkiaSharp;
using Xunit;
using System;

namespace LunaDraw.Tests
{
    public class ShapeRotationTests
    {
        [Fact]
        public void TestRectangleCounterRotationLogic()
        {
            // Setup
            float canvasRotationDegrees = 45f;
            float canvasRotationRadians = canvasRotationDegrees * (float)Math.PI / 180f;
            
            // Create a "UserMatrix" that represents this rotation (and some scale)
            var userMatrix = SKMatrix.CreateRotationDegrees(canvasRotationDegrees);
            
            // Simulate User Input in Screen Space
            // User drags a 100x50 rectangle aligned to the screen
            var screenStart = new SKPoint(0, 0);
            var screenEnd = new SKPoint(100, 50);
            
            // Convert to World Space (Inverse of Canvas Rotation)
            // This mimics what CanvasInputHandler does before passing points to the tool
            userMatrix.TryInvert(out var inverseMatrix);
            var worldStart = inverseMatrix.MapPoint(screenStart);
            var worldEnd = inverseMatrix.MapPoint(screenEnd);
            
            // --- Tool Logic Start ---
            
            // 1. Calculate rotation from CanvasMatrix
            // Note: SkewY/ScaleX extraction
            float extractedRadians = (float)Math.Atan2(userMatrix.SkewY, userMatrix.ScaleX);
            float extractedDegrees = extractedRadians * 180f / (float)Math.PI;
            
            Assert.Equal(canvasRotationDegrees, extractedDegrees, 4); // Verify extraction
            
            // 2. Create alignment matrices
            var toAligned = SKMatrix.CreateRotationDegrees(extractedDegrees);
            var toWorld = SKMatrix.CreateRotationDegrees(-extractedDegrees);

            var p1 = toAligned.MapPoint(worldStart);
            var p2 = toAligned.MapPoint(worldEnd);

            var left = Math.Min(p1.X, p2.X);
            var top = Math.Min(p1.Y, p2.Y);
            var right = Math.Max(p1.X, p2.X);
            var bottom = Math.Max(p1.Y, p2.Y);

            var width = right - left;
            var height = bottom - top;
            
            // Verify Dimensions match Screen Dimensions
            Assert.Equal(100f, width, 4);
            Assert.Equal(50f, height, 4);

            // 3. Top-Left Logic
            var alignedTL = new SKPoint(left, top);
            var worldTL = toWorld.MapPoint(alignedTL);
            var translation = SKMatrix.CreateTranslation(worldTL.X, worldTL.Y);
            
            var finalTransform = SKMatrix.Concat(translation, toWorld);
            
            // --- Verify Transform ---
            
            // The final transform should place the rectangle such that:
            // Local (0,0) -> World Start (0,0)
            // Local (100,0) -> World (approx 70, -70) (Rotated X axis)
            // Local (0,50) -> World (approx 35, 35) (Rotated Y axis)
            
            var testTL = finalTransform.MapPoint(new SKPoint(0, 0));
            Assert.Equal(worldStart.X, testTL.X, 4);
            Assert.Equal(worldStart.Y, testTL.Y, 4);
            
            var testBR = finalTransform.MapPoint(new SKPoint(100, 50));
            Assert.Equal(worldEnd.X, testBR.X, 4);
            Assert.Equal(worldEnd.Y, testBR.Y, 4);
        }
    }
}
