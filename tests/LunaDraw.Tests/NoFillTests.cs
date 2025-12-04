using System.Collections.Generic;
using System.Linq;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Managers;
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
                SelectionManager = new SelectionManager(),
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
                SelectionManager = new SelectionManager(),
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
