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

using System.Collections.Generic;
using System.Linq;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Messages;
using ReactiveUI;
using Moq;
using Xunit;
using SkiaSharp;

namespace LunaDraw.Tests
{
    public class NoFillTests
    {
        private readonly Mock<IMessageBus> mockBus = new Mock<IMessageBus>();

        [Fact]
        public void RectangleTool_Respects_NoFill()
        {
            // Arrange
            var tool = new RectangleTool(mockBus.Object);
            var layer = new Layer();
            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement>(),
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Circle(),
                StrokeColor = SKColors.Black,
                FillColor = null // NO FILL
            };

            // Act
            tool.OnTouchPressed(new SKPoint(0, 0), context);
            tool.OnTouchMoved(new SKPoint(100, 100), context);
            tool.OnTouchReleased(new SKPoint(100, 100), context);

            // Assert
            Assert.Single(layer.Elements);
            var rect = layer.Elements.First() as DrawableRectangle;
            Assert.NotNull(rect);
            Assert.Null(rect.FillColor); // Should be null
        }

        [Fact]
        public void EllipseTool_Respects_NoFill()
        {
            // Arrange
            var tool = new EllipseTool(mockBus.Object);
            var layer = new Layer();
            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement>(),
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Circle(),
                StrokeColor = SKColors.Black,
                FillColor = null // NO FILL
            };

            // Act
            tool.OnTouchPressed(new SKPoint(0, 0), context);
            tool.OnTouchMoved(new SKPoint(100, 100), context);
            tool.OnTouchReleased(new SKPoint(100, 100), context);

            // Assert
            Assert.Single(layer.Elements);
            var ellipse = layer.Elements.First() as DrawableEllipse;
            Assert.NotNull(ellipse);
            Assert.Null(ellipse.FillColor); // Should be null
        }
    }
}
