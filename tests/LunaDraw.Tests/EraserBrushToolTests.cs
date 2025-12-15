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

using System;
using System.Collections.Generic;
using System.Linq;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Messages;
using ReactiveUI;
using Xunit;
using SkiaSharp;
using Moq;

namespace LunaDraw.Tests
{
    public class EraserBrushToolTests
    {
        private readonly Mock<IMessageBus> mockBus;
        private readonly Mock<IPreferencesFacade> mockPreferences;

        public EraserBrushToolTests()
        {
            mockBus = new Mock<IMessageBus>();
            mockPreferences = new Mock<IPreferencesFacade>();
            mockPreferences.Setup(p => p.Get<bool>(AppPreference.IsTransparentBackgroundEnabled)).Returns(false);
            mockPreferences.Setup(p => p.Get(AppPreference.AppTheme)).Returns("Light");
        }

        [Fact]
        public void ErasingTransparentRectangle_ProducesOutlineBlob_NotFilledShape()
        {
            // Arrange
            var rectElement = new DrawableRectangle
            {
                Rectangle = new SKRect(10, 10, 100, 100),
                StrokeColor = SKColors.Red,
                StrokeWidth = 10,
                FillColor = null, // Transparent
                IsVisible = true
            };

            var layer = new Layer();
            layer.Elements.Add(rectElement);

            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement> { rectElement },
                StrokeWidth = 10,
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Circle()
            };

            var tool = new EraserBrushTool(mockBus.Object, mockPreferences.Object);

            // Act
            // Simulate erasing across the top border
            tool.OnTouchPressed(new SKPoint(50, 0), context);
            tool.OnTouchReleased(new SKPoint(50, 20), context);

            // Assert
            // The original element should be removed and replaced
            Assert.DoesNotContain(rectElement, layer.Elements);
            Assert.Single(layer.Elements);

            var resultElement = layer.Elements.First() as DrawablePath;
            Assert.NotNull(resultElement);

            // If bug is present:
            // resultElement.StrokeWidth will be 10 (original width)
            // resultElement.Path will cover the whole area (10,10,100,100)
            // resultElement.IsFilled will be true
            // Draw() will fill the area with Red (fallback)

            // If bug is fixed:
            // resultElement.StrokeWidth will be 0 (it's a filled blob of the outline)
            // resultElement.Path will cover only the border area

            Assert.True(resultElement.StrokeWidth == 0, "Resulting element should be a filled blob (StrokeWidth 0) representing the remaining outline, but it had a stroke width (implying it remained a shape).");
        }

        [Fact]
        public void ErasingSingleStampDot_RemovesElementCompletely()
        {
            // Arrange
            var stamps = new DrawableStamps
            {
                Points = new List<SKPoint> { new SKPoint(100, 100) },
                Size = 20,
                IsVisible = true,
                Shape = BrushShape.Circle()
            };

            var layer = new Layer();
            layer.Elements.Add(stamps);

            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement> { stamps },
                StrokeWidth = 30, // Eraser larger than stamp (20)
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Circle()
            };

            var tool = new EraserBrushTool(mockBus.Object, mockPreferences.Object);

            // Act
            // Erase exactly over the stamp
            tool.OnTouchPressed(new SKPoint(100, 100), context);
            tool.OnTouchReleased(new SKPoint(100, 100), context);

            // Assert
            Assert.Empty(layer.Elements);
        }

        [Fact]
        public void ErasingFreehandDot_RemovesElementCompletely()
        {
            // Arrange
            var path = new SKPath();
            path.MoveTo(100, 100);
            path.LineTo(100.1f, 100.1f); // Tiny line (dot)

            var freehand = new DrawablePath
            {
                Path = path,
                StrokeWidth = 20,
                IsFilled = false,
                StrokeColor = SKColors.Black,
                IsVisible = true
            };

            var layer = new Layer();
            layer.Elements.Add(freehand);

            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement> { freehand },
                StrokeWidth = 30, // Eraser larger than dot
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Circle()
            };

            var tool = new EraserBrushTool(mockBus.Object, mockPreferences.Object);

            // Act
            tool.OnTouchPressed(new SKPoint(100, 100), context);
            tool.OnTouchReleased(new SKPoint(100, 100), context);

            // Assert
            Assert.Empty(layer.Elements);
        }
    }
}
