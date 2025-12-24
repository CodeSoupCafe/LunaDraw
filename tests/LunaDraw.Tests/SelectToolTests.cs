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
            var selectionObserver = new SelectionObserver();
            if (initiallySelected)
            {
                selectionObserver.Add(element);
            }

            var layer = new Layer();
            layer.Elements.Add(element);

            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = elements,
                Layers = new List<Layer> { layer },
                SelectionObserver = selectionObserver,
                BrushShape = BrushShape.Circle()
            };
            var tool = new SelectTool(mockBus.Object);
            var point = new SKPoint(100, 100);

            // Act
            tool.OnTouchPressed(point, context);

            // Assert
            Assert.Equal(expectedSelected, selectionObserver.Contains(element));
        }

        private class TestDrawableElement : IDrawableElement
        {
            public Guid Id { get; init; } = Guid.NewGuid();
            public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
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
            public float AnimationProgress { get; set; } = 1.0f;
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
            public SKPath GetGeometryPath() => new SKPath();
        }
    }
}
