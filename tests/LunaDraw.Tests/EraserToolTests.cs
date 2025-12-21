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
    public class EraserToolTests
    {
        private readonly Mock<IMessageBus> mockBus;

        public EraserToolTests()
        {
            mockBus = new Mock<IMessageBus>();
        }

        [Fact]
        public void OnTouchPressed_ErasesElement_WhenHit()
        {
            // Arrange
            var element = new TestDrawableElement { IsVisible = true, ZIndex = 1, HitTestResult = true };
            var layer = new Layer();
            layer.Elements.Add(element);

            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement> { element },
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Circle()
            };

            var tool = new EraserTool(mockBus.Object);
            var point = new SKPoint(10, 10);

            // Act
            tool.OnTouchPressed(point, context);

            // Assert
            Assert.DoesNotContain(element, layer.Elements);
        }

        [Fact]
        public void OnTouchMoved_ErasesElement_WhenErasing()
        {
            // Arrange
            var element = new TestDrawableElement { IsVisible = true, ZIndex = 1, HitTestResult = true };
            var layer = new Layer();
            layer.Elements.Add(element);

            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement> { element },
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Circle()
            };

            var tool = new EraserTool(mockBus.Object);
            var point = new SKPoint(10, 10);

            // Act
            // First press to start erasing state
            tool.OnTouchPressed(new SKPoint(0, 0), context);
            // Move to hit the element
            tool.OnTouchMoved(point, context);

            // Assert
            Assert.DoesNotContain(element, layer.Elements);
        }

        [Fact]
        public void OnTouchMoved_DoesNotErase_WhenNotErasing()
        {
            // Arrange
            var element = new TestDrawableElement { IsVisible = true, ZIndex = 1, HitTestResult = true };
            var layer = new Layer();
            layer.Elements.Add(element);

            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement> { element },
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Circle()
            };

            var tool = new EraserTool(mockBus.Object);
            var point = new SKPoint(10, 10);

            // Act
            // Move without pressing first
            tool.OnTouchMoved(point, context);

            // Assert
            Assert.Contains(element, layer.Elements);
        }

        [Fact]
        public void OnTouchReleased_StopsErasing()
        {
            // Arrange
            var element = new TestDrawableElement { IsVisible = true, ZIndex = 1, HitTestResult = false }; // Start with False so initial press doesn't erase
            var layer = new Layer();
            layer.Elements.Add(element);

            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement> { element },
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Circle()
            };

            var tool = new EraserTool(mockBus.Object);

            // Act
            tool.OnTouchPressed(new SKPoint(0, 0), context); // Start erasing (misses element)

            tool.OnTouchReleased(new SKPoint(0, 0), context); // Stop erasing

            element.HitTestResult = true; // Now it would hit if we were erasing
            tool.OnTouchMoved(new SKPoint(10, 10), context); // Move over element

            // Assert
            Assert.Contains(element, layer.Elements);
        }

        private class TestDrawableElement : IDrawableElement
        {
            public Guid Id { get; init; } = Guid.NewGuid();
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
            public SKPath GetGeometryPath() => new SKPath();
        }
    }
}
