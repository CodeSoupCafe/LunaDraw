using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Services;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Managers;
using ReactiveUI;
using SkiaSharp;
using Xunit;
using Moq; // ADDED: Required for Mock<>

namespace LunaDraw.Tests
{
    public class ShapeToolsTests
    {
        private static readonly Mock<IMessageBus> MockBus = new Mock<IMessageBus>();

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
                SelectionManager = new SelectionManager(),
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
        [MemberData(nameof(ToolTypesData))]
        public void OnTouchReleased_ShouldAddOneElement(IDrawingTool tool, Type expectedElementType)
        {
            // Arrange
            var layer = new Layer();

            // Act
            PerformDrawAction(tool, layer);

            // Assert
            layer.Elements.Should().HaveCount(1);
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
            layer.Elements.First().Should().BeOfType(expectedElementType);
        }

        [Theory]
        [MemberData(nameof(ToolTypesData))]
        public void OnTouchReleased_ShouldSetStrokeColor(IDrawingTool tool, Type expectedElementType)
        {
            // Arrange
            var layer = new Layer();

            // Act
            PerformDrawAction(tool, layer);

            // Assert
            layer.Elements.First().StrokeColor.Should().Be(SKColors.Red);
        }

        [Theory]
        [MemberData(nameof(ToolTypesData))]
        public void OnTouchReleased_ShouldSetStrokeWidth(IDrawingTool tool, Type expectedElementType)
        {
            // Arrange
            var layer = new Layer();

            // Act
            PerformDrawAction(tool, layer);

            // Assert
            layer.Elements.First().StrokeWidth.Should().Be(2f);
        }
        
        [Theory]
        [MemberData(nameof(ToolTypesData))]
        public void OnTouchCancelled_ShouldNotAddShape(IDrawingTool tool, Type expectedElementType)
        {
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
            layer.Elements.Should().BeEmpty();
        }
    }
}