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
using System.Collections.ObjectModel;
using System.Linq;

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
// Removed: using LunaDraw.Logic.Services; // ADDED: Required for IMessageBus
using Moq;
using SkiaSharp;
using Xunit;
using ReactiveUI; // ADDED: Required for IMessageBus

namespace LunaDraw.Tests
{
    public class FillToolTests
    {
        private readonly Mock<IMessageBus> mockMessageBus;
        private readonly FillTool fillTool;
        private readonly SelectionObserver selectionObserver; // Added for ToolContext
        private readonly Layer defaultLayer; // Added for ToolContext
        private readonly BrushShape defaultBrushShape; // Added for ToolContext

        public FillToolTests()
        {
            mockMessageBus = new Mock<IMessageBus>();
            fillTool = new FillTool(mockMessageBus.Object);
            selectionObserver = new SelectionObserver(); // Initialize SelectionObserver
            defaultLayer = new Layer(); // Initialize default layer
            defaultBrushShape = BrushShape.Circle(); // Initialize default brush shape
        }

        private ToolContext CreateDefaultToolContext()
        {
            return new ToolContext
            {
                CurrentLayer = defaultLayer,
                BrushShape = defaultBrushShape,
                AllElements = new List<IDrawableElement>(), // Default to empty list
                SelectionObserver = selectionObserver
            };
        }

        [Fact]
        public void Name_ShouldReturnCorrectValue()
        {
            // Arrange
            // Act
            var name = fillTool.Name;

            // Assert
            Assert.Equal("Fill", name);
        }

        [Fact]
        public void Type_ShouldReturnCorrectValue()
        {
            // Arrange
            // Act
            var type = fillTool.Type;

            // Assert
            Assert.Equal(ToolType.Fill, type);
        }

        [Fact]
        public void OnTouchPressed_ShouldDoNothingIfLayerIsLocked()
        {
            // Arrange
            var lockedLayer = new Layer { IsLocked = true };
            var context = CreateDefaultToolContext();
            context.CurrentLayer = lockedLayer; // Override CurrentLayer
            var point = new SKPoint(10, 10);

            // Act
            fillTool.OnTouchPressed(point, context);

            // Assert
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Never);
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<DrawingStateChangedMessage>()), Times.Never);
        }

        [Fact]
        public void OnTouchPressed_ShouldDoNothingIfNoElementIsHit()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var mockElement = new Mock<IDrawableElement>();
            mockElement.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(false); // Element not hit
            mockElement.Setup(e => e.IsVisible).Returns(true);
            var context = CreateDefaultToolContext();
            context.CurrentLayer = unlockedLayer;
            context.AllElements = new List<IDrawableElement> { mockElement.Object };
            var point = new SKPoint(10, 10);

            // Act
            fillTool.OnTouchPressed(point, context);

            // Assert
            mockElement.VerifySet(e => e.FillColor = It.IsAny<SKColor?>(), Times.Never);
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Never);
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<DrawingStateChangedMessage>()), Times.Never);
        }

        [Fact]
        public void OnTouchPressed_ShouldSetFillColorOfHitElement()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var expectedFillColor = SKColors.Red;
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties(); // Allow setting FillColor
            mockElement.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(true); // Element is hit
            mockElement.Setup(e => e.IsVisible).Returns(true);
            var context = CreateDefaultToolContext();
            context.CurrentLayer = unlockedLayer;
            context.AllElements = new List<IDrawableElement> { mockElement.Object };
            context.FillColor = expectedFillColor;
            var point = new SKPoint(10, 10);

            // Act
            fillTool.OnTouchPressed(point, context);

            // Assert
            Assert.Equal(expectedFillColor, mockElement.Object.FillColor);
        }

        [Fact]
        public void OnTouchPressed_ShouldSendCanvasInvalidateMessage()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            mockElement.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(true);
            mockElement.Setup(e => e.IsVisible).Returns(true);
            var context = CreateDefaultToolContext();
            context.CurrentLayer = unlockedLayer;
            context.AllElements = new List<IDrawableElement> { mockElement.Object };
            var point = new SKPoint(10, 10);

            // Act
            fillTool.OnTouchPressed(point, context);

            // Assert
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Once);
        }

        [Fact]
        public void OnTouchPressed_ShouldSendDrawingStateChangedMessage()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            mockElement.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(true);
            mockElement.Setup(e => e.IsVisible).Returns(true);
            var context = CreateDefaultToolContext();
            context.CurrentLayer = unlockedLayer;
            context.AllElements = new List<IDrawableElement> { mockElement.Object };
            var point = new SKPoint(10, 10);

            // Act
            fillTool.OnTouchPressed(point, context);

            // Assert
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<DrawingStateChangedMessage>()), Times.Once);
        }

        [Fact]
        public void OnTouchPressed_ShouldFillTopmostVisibleElement()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var expectedFillColor = SKColors.Blue;

            var mockElementBottom = new Mock<IDrawableElement>();
            mockElementBottom.SetupAllProperties();
            mockElementBottom.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(true);
            mockElementBottom.Setup(e => e.IsVisible).Returns(true);
            mockElementBottom.Setup(e => e.ZIndex).Returns(0);
            mockElementBottom.Object.FillColor = SKColors.Black;

            var mockElementTop = new Mock<IDrawableElement>();
            mockElementTop.SetupAllProperties();
            mockElementTop.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(true);
            mockElementTop.Setup(e => e.IsVisible).Returns(true);
            mockElementTop.Setup(e => e.ZIndex).Returns(1);
            mockElementTop.Object.FillColor = SKColors.Black;

            var mockElementInvisible = new Mock<IDrawableElement>();
            mockElementInvisible.SetupAllProperties();
            mockElementInvisible.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(true);
            mockElementInvisible.Setup(e => e.IsVisible).Returns(false); // Invisible
            mockElementInvisible.Setup(e => e.ZIndex).Returns(2);
            mockElementInvisible.Object.FillColor = SKColors.Black;

            var context = CreateDefaultToolContext();
            context.CurrentLayer = unlockedLayer;
            context.AllElements = new List<IDrawableElement> { mockElementBottom.Object, mockElementTop.Object, mockElementInvisible.Object };
            context.FillColor = expectedFillColor;
            var point = new SKPoint(10, 10);

            // Act
            fillTool.OnTouchPressed(point, context);

            // Assert
            Assert.Equal(expectedFillColor, mockElementTop.Object.FillColor);
            Assert.Equal(SKColors.Black, mockElementBottom.Object.FillColor);
            Assert.Equal(SKColors.Black, mockElementInvisible.Object.FillColor); // Should not be filled
        }

        [Fact]
        public void OnTouchMoved_ShouldDoNothing()
        {
            // Arrange
            var context = CreateDefaultToolContext();
            var point = new SKPoint(10, 10);

            // Act
            fillTool.OnTouchMoved(point, context);

            // Assert (No exceptions, no messages sent, no state change)
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public void OnTouchReleased_ShouldDoNothing()
        {
            // Arrange
            var context = CreateDefaultToolContext();
            var point = new SKPoint(10, 10);

            // Act
            fillTool.OnTouchReleased(point, context);

            // Assert (No exceptions, no messages sent, no state change)
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public void OnTouchCancelled_ShouldDoNothing()
        {
            // Arrange
            var context = CreateDefaultToolContext();

            // Act
            fillTool.OnTouchCancelled(context);

            // Assert (No exceptions, no messages sent, no state change)
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<object>()), Times.Never);
        }
    }
}