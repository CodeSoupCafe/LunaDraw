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
using ReactiveUI;
using SkiaSharp;
using Xunit;
using Moq; // ADDED: Required for Mock<>

namespace LunaDraw.Tests
{
    public class ShapeToolsTests
    {
        private static readonly Mock<IMessageBus> MockBus = new Mock<IMessageBus>();

        public static TheoryData<IDrawingTool> ToolData =>
            new TheoryData<IDrawingTool>
            {
                { new RectangleTool(MockBus.Object)},
                { new EllipseTool(MockBus.Object) },
                { new LineTool(MockBus.Object) }
            };

        public static TheoryData<IDrawingTool, Type> ToolTypesData =>
            new TheoryData<IDrawingTool, Type>
            {
                { new RectangleTool(MockBus.Object), typeof(DrawableRectangle) },
                { new EllipseTool(MockBus.Object), typeof(DrawableEllipse) },
                { new LineTool(MockBus.Object), typeof(DrawableLine) }
            };

        private void PerformDrawAction(IDrawingTool tool, Layer layer)
        {
            // Arrange
            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement>(),
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Circle(),
                StrokeColor = SKColors.Red,
                StrokeWidth = 2f
            };

            // Act
            tool.OnTouchPressed(new SKPoint(10, 10), context);
            tool.OnTouchMoved(new SKPoint(50, 50), context);
            tool.OnTouchReleased(new SKPoint(50, 50), context);
        }

        [Theory]
        [MemberData(nameof(ToolData))]
        public void OnTouchReleased_ShouldAddOneElement(IDrawingTool tool)
        {
            // Arrange
            var layer = new Layer();

            // Act
            PerformDrawAction(tool, layer);

            // Assert
            Assert.Single(layer.Elements);
        }

        [Theory]
        [MemberData(nameof(ToolTypesData))]
        public void OnTouchReleased_ShouldAddCorrectType(IDrawingTool tool, Type expectedElementType)
        {
            // Arrange
            var layer = new Layer();

            // Act
            PerformDrawAction(tool, layer);

            // Assert
            Assert.IsType(expectedElementType, layer.Elements.First());
        }

        [Theory]
        [MemberData(nameof(ToolData))]
        public void OnTouchReleased_ShouldSetStrokeColor(IDrawingTool tool)
        {
            // Arrange
            var layer = new Layer();

            // Act
            PerformDrawAction(tool, layer);

            // Assert
            Assert.Equal(SKColors.Red, layer.Elements.First().StrokeColor);
        }

        [Theory]
        [MemberData(nameof(ToolData))]
        public void OnTouchReleased_ShouldSetStrokeWidth(IDrawingTool tool)
        {
            // Arrange
            var layer = new Layer();

            // Act
            PerformDrawAction(tool, layer);

            // Assert
            Assert.Equal(2f, layer.Elements.First().StrokeWidth);
        }

        [Theory]
        [MemberData(nameof(ToolData))]
        public void OnTouchCancelled_ShouldNotAddShape(IDrawingTool tool)
        {
            // Arrange
            var layer = new Layer();
            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement>(),
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Circle()
            };

            // Act
            tool.OnTouchPressed(new SKPoint(10, 10), context);
            tool.OnTouchMoved(new SKPoint(50, 50), context);
            tool.OnTouchCancelled(context);

            // Assert
            Assert.Empty(layer.Elements);
        }
    }
}
