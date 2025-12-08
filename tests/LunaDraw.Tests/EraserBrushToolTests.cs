using System;
using System.Collections.Generic;
using System.Linq;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Managers;
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

        public EraserBrushToolTests()
        {
            mockBus = new Mock<IMessageBus>();
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
                SelectionManager = new SelectionManager(),
                BrushShape = BrushShape.Circle()
            };
            
            var tool = new EraserBrushTool(mockBus.Object);
            
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
                SelectionManager = new SelectionManager(),
                BrushShape = BrushShape.Circle()
            };

            var tool = new EraserBrushTool(mockBus.Object);

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
                SelectionManager = new SelectionManager(),
                BrushShape = BrushShape.Circle()
            };

            var tool = new EraserBrushTool(mockBus.Object);

            // Act
            tool.OnTouchPressed(new SKPoint(100, 100), context);
            tool.OnTouchReleased(new SKPoint(100, 100), context);

            // Assert
            Assert.Empty(layer.Elements);
        }
    }
}
