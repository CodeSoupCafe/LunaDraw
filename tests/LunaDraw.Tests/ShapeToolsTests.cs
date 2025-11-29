using System;
using System.Collections.Generic;
using System.Linq;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Managers;
using Xunit;
using SkiaSharp;

namespace LunaDraw.Tests
{
    public class ShapeToolsTests
    {
        public static TheoryData<IDrawingTool, Type> ToolTypesData =>
            new TheoryData<IDrawingTool, Type>
            {
                { new RectangleTool(), typeof(DrawableRectangle) },
                { new EllipseTool(), typeof(DrawableEllipse) },
                { new LineTool(), typeof(DrawableLine) }
            };

        [Theory]
        [MemberData(nameof(ToolTypesData))]
        public void OnTouchReleased_AddsCorrectShapeToLayer(IDrawingTool tool, Type expectedElementType)
        {
            // Arrange
            var layer = new Layer();
            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement>(),
                SelectionManager = new SelectionManager(),
                BrushShape = BrushShape.Circle(),
                StrokeColor = SKColors.Red,
                StrokeWidth = 2f
            };

            // Act
            // 1. Press to start
            tool.OnTouchPressed(new SKPoint(10, 10), context);
            
            // 2. Move to define shape
            tool.OnTouchMoved(new SKPoint(50, 50), context);

            // 3. Release to finalize
            tool.OnTouchReleased(new SKPoint(50, 50), context);

            // Assert
            Assert.Single(layer.Elements);
            var element = layer.Elements.First();
            Assert.IsType(expectedElementType, element);
            Assert.Equal(SKColors.Red, element.StrokeColor);
            Assert.Equal(2f, element.StrokeWidth);
            
            // Verify bounds (approximate for line/shapes)
            // Rect/Ellipse should be from 10,10 to 50,50.
            if (tool is LineTool)
            {
                // LineTool sets Start/End relative to 0,0 and uses TransformMatrix to position
                var line = element as DrawableLine;
                Assert.Equal(SKPoint.Empty, line.StartPoint);
                Assert.Equal(new SKPoint(40, 40), line.EndPoint); // 50-10 = 40
                
                // Verify Transform has translation
                Assert.Equal(10, line.TransformMatrix.TransX);
                Assert.Equal(10, line.TransformMatrix.TransY);
            }
            else
            {
                Assert.Equal(10, element.Bounds.Left);
                Assert.Equal(10, element.Bounds.Top);
                Assert.Equal(50, element.Bounds.Right);
                Assert.Equal(50, element.Bounds.Bottom);
            }
        }

        [Theory]
        [MemberData(nameof(ToolTypesData))]
        public void OnTouchCancelled_DoesNotAddShape(IDrawingTool tool, Type expectedElementType)
        {
            _ = expectedElementType; // Silence unused parameter warning

            // Arrange
            var layer = new Layer();
            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement>(),
                SelectionManager = new SelectionManager(),
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