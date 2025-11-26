using System;
using System.Collections.Generic;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Managers;
using Xunit;
using SkiaSharp;

namespace LunaDraw.Tests
{
    public class SelectToolTests
    {
        [Fact]
        public void OnTouchPressed_SelectsElement_WhenHitTestTrue()
        {
            // Arrange
            var element = new TestDrawableElement { IsVisible = true, ZIndex = 1 };
            var elements = new List<IDrawableElement> { element };
            var selectionManager = new SelectionManager();
            var context = new ToolContext
            {
                CurrentLayer = new Layer(),
                AllElements = elements,
                SelectionManager = selectionManager
            };
            var tool = new SelectTool();
            var point = new SKPoint(10, 10);
            element.HitTestResult = true;

            // Act
            tool.OnTouchPressed(point, context);

            // Assert
            Assert.True(selectionManager.Contains(element));
        }

        [Fact]
        public void OnTouchPressed_ClearsSelection_WhenNoElementHit()
        {
            // Arrange
            var element = new TestDrawableElement { IsVisible = true, ZIndex = 1 };
            var elements = new List<IDrawableElement> { element };
            var selectionManager = new SelectionManager();
            selectionManager.Add(element);
            var context = new ToolContext
            {
                CurrentLayer = new Layer(),
                AllElements = elements,
                SelectionManager = selectionManager
            };
            var tool = new SelectTool();
            var point = new SKPoint(100, 100);
            element.HitTestResult = false;

            // Act
            tool.OnTouchPressed(point, context);

            // Assert
            Assert.False(selectionManager.Contains(element));
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
            public SKColor StrokeColor { get; set; } = SKColors.Black;
            public float StrokeWidth { get; set; } = 1f;
            public bool HitTestResult { get; set; }
            public bool HitTest(SKPoint point) => HitTestResult;
            public void Draw(SKCanvas canvas) { }
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
