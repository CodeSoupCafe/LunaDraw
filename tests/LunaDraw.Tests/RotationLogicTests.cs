/* 
 *  Copyright (c) 2025 CodeSoupCafe LLC
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 *  
 */

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
