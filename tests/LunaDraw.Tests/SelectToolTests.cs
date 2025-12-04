using System;
using System.Collections.Generic;
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
    public class SelectToolTests
    {
        private readonly Mock<IMessageBus> mockBus = new Mock<IMessageBus>();

        [Theory]
        [InlineData(false, true, true)]  // Initially not selected, Hit -> Selected
        [InlineData(true, false, false)] // Initially selected, No Hit -> Deselected
        public void OnTouchPressed_UpdatesSelection_Correctly(bool initiallySelected, bool hit, bool expectedSelected)
        {
            // Arrange
            var element = new TestDrawableElement { IsVisible = true, ZIndex = 1 };
            element.HitTestResult = hit;

            var elements = new List<IDrawableElement> { element };
            var selectionManager = new SelectionManager();
            if (initiallySelected)
            {
                selectionManager.Add(element);
            }

            var context = new ToolContext
            {
                CurrentLayer = new Layer(),
                AllElements = elements,
                SelectionManager = selectionManager,
                BrushShape = BrushShape.Circle()
            };
            var tool = new SelectTool(mockBus.Object);
            var point = new SKPoint(100, 100);

            // Act
            tool.OnTouchPressed(point, context);

            // Assert
            Assert.Equal(expectedSelected, selectionManager.Contains(element));
        }

        private class TestDrawableElement : IDrawableElement
        {
            public Guid Id { get; } = Guid.NewGuid();
            public SKRect Bounds => new SKRect(0, 0, 20, 20);
            public SKMatrix TransformMatrix { get; set; } = SKMatrix.CreateIdentity();
            public bool IsVisible { get; set; }
            public bool IsSelected { get; set; }
            public int ZIndex { get; set; }
            public byte Opacity { get; set; } = 255;
            public SKColor? FillColor { get; set; } = null;
        public SKColor StrokeColor { get; set; }
        public float StrokeWidth { get; set; }
        public bool IsGlowEnabled { get; set; }
        public SKColor GlowColor { get; set; }
        public float GlowRadius { get; set; }
        public bool HitTestResult { get; set; }

        public void Draw(SKCanvas canvas) { }
        public bool HitTest(SKPoint point) => HitTestResult;
            public IDrawableElement Clone() => new TestDrawableElement
            {
                IsVisible = this.IsVisible,
                IsSelected = this.IsSelected,
                ZIndex = this.ZIndex,
                Opacity = this.Opacity,
                FillColor = this.FillColor,
                StrokeColor = this.StrokeColor,
                StrokeWidth = this.StrokeWidth,
                HitTestResult = this.HitTestResult
            };
            public void Translate(SKPoint offset) { }
            public void Transform(SKMatrix matrix) { }
            public SKPath GetPath() => new SKPath();
        }
    }
}
