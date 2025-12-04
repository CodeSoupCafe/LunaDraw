using SkiaSharp;
using Xunit;
using LunaDraw.Logic.Extensions;
using LunaDraw.Logic.Models;
using System;

namespace LunaDraw.Tests
{
    public class RotationLogicTests
    {
        [Fact]
        public void GetRotationDegrees_ReturnsCorrectAngle()
        {
            float angle = 45f;
            var matrix = SKMatrix.CreateRotationDegrees(angle);
            
            float extracted = matrix.GetRotationDegrees();
            
            Assert.Equal(angle, extracted, 3);
        }

        [Fact]
        public void GetRotationDegrees_ReturnsCorrectAngle_Negative()
        {
            float angle = -30f;
            var matrix = SKMatrix.CreateRotationDegrees(angle);
            
            float extracted = matrix.GetRotationDegrees();
            
            Assert.Equal(angle, extracted, 3);
        }

        [Fact]
        public void GetRotationDegrees_ReturnsCorrectAngle_WithScale()
        {
            float angle = 90f;
            var matrix = SKMatrix.CreateRotationDegrees(angle);
            matrix = matrix.PostConcat(SKMatrix.CreateScale(2.0f, 2.0f));
            
            float extracted = matrix.GetRotationDegrees();
            
            Assert.Equal(angle, extracted, 3);
        }

        [Fact]
        public void DrawableStamps_RotationsProperty_UpdatesAndCopies()
        {
            var stamps = new DrawableStamps();
            stamps.Rotations = new System.Collections.Generic.List<float> { 10f, 20f, 30f };

            Assert.Equal(3, stamps.Rotations.Count);
            Assert.Equal(10f, stamps.Rotations[0]);

            var clone = (DrawableStamps)stamps.Clone();
            Assert.Equal(3, clone.Rotations.Count);
            Assert.Equal(20f, clone.Rotations[1]);
            Assert.NotSame(stamps.Rotations, clone.Rotations); // Ensure deep copy of list
        }
    }
}
