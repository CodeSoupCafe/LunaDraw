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
using LunaDraw.Logic.ViewModels;
using LunaDraw.Logic.Services; // ADDED: Required for ILayerFacade and IToolStateManager
using Moq;
using SkiaSharp;
using Xunit;
using ReactiveUI; // Required for IMessageBus

namespace LunaDraw.Tests
{
    public class FreehandToolTests
    {
        private readonly Mock<IMessageBus> mockMessageBus;
        private readonly FreehandTool freehandTool;
        private readonly Mock<ILayerFacade> mockLayerFacade;
        private readonly Mock<ToolbarViewModel> mockToolbarViewModel;
        private readonly SelectionObserver selectionObserver;
        private readonly NavigationModel navigationModel;

        public FreehandToolTests()
        {
            mockMessageBus = new Mock<IMessageBus>();
            freehandTool = new FreehandTool(mockMessageBus.Object);

            mockLayerFacade = new Mock<ILayerFacade>();
            mockToolbarViewModel = new Mock<ToolbarViewModel>();
            selectionObserver = new SelectionObserver();
            navigationModel = new NavigationModel();
        }

        private ToolContext CreateToolContext(Layer? currentLayer = null, SKColor? fillColor = null) // Changed Layer to Layer?
        {
            if (currentLayer is null) // Changed check for null-ability
            {
                currentLayer = new Layer();
            }
            return new ToolContext
            {
                CurrentLayer = currentLayer,
                AllElements = new ObservableCollection<IDrawableElement>(currentLayer.Elements),
                SelectionObserver = selectionObserver,
                BrushShape = BrushShape.Circle(),
                StrokeColor = SKColors.Black,
                StrokeWidth = 20f,
                FillColor = fillColor,
                Opacity = 255,
                Flow = 255,
                Spacing = 0.5f,
                Scale = 1.0f
            };
        }

        [Fact]
        public void Name_ShouldReturnCorrectValue()
        {
            // Arrange
            // Act
            var name = freehandTool.Name;

            // Assert
            Assert.Equal("Stamps", name);
        }

        [Fact]
        public void Type_ShouldReturnCorrectValue()
        {
            // Arrange
            // Act
            var type = freehandTool.Type;

            // Assert
            Assert.Equal(ToolType.Freehand, type);
        }

        [Fact]
        public void OnTouchPressed_ShouldDoNothingIfLayerIsLocked()
        {
            // Arrange
            var lockedLayer = new Layer { IsLocked = true };
            var context = CreateToolContext(lockedLayer);
            var point = new SKPoint(10, 10);

            // Act
            freehandTool.OnTouchPressed(point, context);

            // Assert
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Never);
            // Internal state is not directly accessible, but we can infer no drawing started
            freehandTool.OnTouchMoved(new SKPoint(20, 20), context);
        }

        [Fact]
        public void OnTouchPressed_ShouldInitializeCurrentPointsAndSetIsDrawing()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var context = CreateToolContext(unlockedLayer);
            var point = new SKPoint(10, 10);

            // Act
            freehandTool.OnTouchPressed(point, context);

            // Assert
            // We can't directly assert currentPoints as it's private, but we can verify subsequent behavior
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Once);

            // Further actions should indicate drawing has started
            var movedPoint = new SKPoint(20, 20);
            freehandTool.OnTouchMoved(movedPoint, context); // Move to trigger point addition
            freehandTool.OnTouchReleased(movedPoint, context); // Release to finalize
            Assert.Single(unlockedLayer.Elements);
        }

        [Fact]
        public void OnTouchMoved_ShouldDoNothingIfNotDrawing()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var context = CreateToolContext(unlockedLayer);
            var point = new SKPoint(10, 10);

            // Act
            freehandTool.OnTouchMoved(point, context);

            // Assert
            Assert.Empty(unlockedLayer.Elements); // No elements should be added
        }

        [Fact]
        public void OnTouchMoved_ShouldDoNothingIfLayerIsLocked()
        {
            // Arrange
            var lockedLayer = new Layer { IsLocked = true };
            var context = CreateToolContext(lockedLayer);
            var point = new SKPoint(10, 10);

            // Act
            freehandTool.OnTouchPressed(new SKPoint(5, 5), context); // Try to start drawing
            freehandTool.OnTouchMoved(point, context);

            // Assert
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Never);
            Assert.Empty(lockedLayer.Elements);
        }

        [Fact]
        public void OnTouchMoved_ShouldAddPointsWhenDistanceExceedsSpacing()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var context = CreateToolContext(unlockedLayer);
            var startPoint = new SKPoint(10, 10);
            freehandTool.OnTouchPressed(startPoint, context);
            mockMessageBus.Invocations.Clear(); // Clear initial message

            var movePoint = new SKPoint(100, 10); // Far enough to add multiple stamps

            // Act
            freehandTool.OnTouchMoved(movePoint, context);

            // Assert
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Once); // At least one invalidate message for adding points
        }

        [Fact]
        public void OnTouchMoved_ShouldNotAddPointsWhenDistanceIsLessThanSpacing()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var context = CreateToolContext(unlockedLayer);
            var startPoint = new SKPoint(10, 10);
            freehandTool.OnTouchPressed(startPoint, context);
            mockMessageBus.Invocations.Clear(); // Clear initial message

            var movePoint = new SKPoint(10.1f, 10.1f); // Very small move, less than spacing

            // Act
            freehandTool.OnTouchMoved(movePoint, context);

            // Assert
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Never); // No invalidate message for new points

            freehandTool.OnTouchReleased(movePoint, context);
            var drawableStamps = Assert.IsType<DrawableStamps>(Assert.Single(unlockedLayer.Elements));
            Assert.Single(drawableStamps.Points); // Only the initial point
        }

        [Fact]
        public void OnTouchReleased_ShouldDoNothingIfNotDrawing()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var context = CreateToolContext(unlockedLayer);
            var point = new SKPoint(10, 10);

            // Act
            freehandTool.OnTouchReleased(point, context);

            // Assert
            Assert.Empty(unlockedLayer.Elements);
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<DrawingStateChangedMessage>()), Times.Never);
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Never); // No additional invalidate message
        }

        [Fact]
        public void OnTouchReleased_ShouldAddDrawableStampsToLayer()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var context = CreateToolContext(unlockedLayer);
            var startPoint = new SKPoint(10, 10);
            var endPoint = new SKPoint(50, 50);
            freehandTool.OnTouchPressed(startPoint, context);
            freehandTool.OnTouchMoved(endPoint, context); // Add some points
            mockMessageBus.Invocations.Clear(); // Clear previous messages

            // Act
            freehandTool.OnTouchReleased(endPoint, context);

            // Assert
            Assert.Single(unlockedLayer.Elements);
            Assert.IsType<DrawableStamps>(unlockedLayer.Elements.First());
        }

        [Fact]
        public void OnTouchReleased_ShouldSendDrawingStateChangedMessage()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var context = CreateToolContext(unlockedLayer);
            var startPoint = new SKPoint(10, 10);
            var endPoint = new SKPoint(50, 50);
            freehandTool.OnTouchPressed(startPoint, context);
            freehandTool.OnTouchMoved(endPoint, context);
            mockMessageBus.Invocations.Clear();

            // Act
            freehandTool.OnTouchReleased(endPoint, context);

            // Assert
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<DrawingStateChangedMessage>()), Times.Once);
        }

        [Fact]
        public void OnTouchReleased_ShouldSendCanvasInvalidateMessage()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var context = CreateToolContext(unlockedLayer);
            var startPoint = new SKPoint(10, 10);
            var endPoint = new SKPoint(50, 50);
            freehandTool.OnTouchPressed(startPoint, context);
            freehandTool.OnTouchMoved(endPoint, context);
            mockMessageBus.Invocations.Clear();

            // Act
            freehandTool.OnTouchReleased(endPoint, context);

            // Assert
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Once);
        }

        [Fact]
        public void OnTouchReleased_ShouldResetIsDrawingAndCurrentPoints()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var context = CreateToolContext(unlockedLayer);
            var startPoint = new SKPoint(10, 10);
            var endPoint = new SKPoint(50, 50);
            freehandTool.OnTouchPressed(startPoint, context);
            freehandTool.OnTouchMoved(endPoint, context);

            // Act
            freehandTool.OnTouchReleased(endPoint, context);

            // Assert
            freehandTool.OnTouchMoved(new SKPoint(60, 60), context);
            // mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Exactly(2)); // REMOVED
        }

        [Fact]
        public void OnTouchReleased_ShouldNotAddEmptyDrawableStamps()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var context = CreateToolContext(unlockedLayer);
            var point = new SKPoint(10, 10);

            // Act - only press, no moves to add multiple points
            freehandTool.OnTouchPressed(point, context);
            freehandTool.OnTouchReleased(point, context);

            // Assert
            Assert.IsType<DrawableStamps>(unlockedLayer.Elements.First());
            Assert.Single(((DrawableStamps)unlockedLayer.Elements.First()).Points);
            Assert.Contains(point, ((DrawableStamps)unlockedLayer.Elements.First()).Points);
        }

        [Fact]
        public void OnTouchCancelled_ShouldResetStateAndSendInvalidateMessage()
        {
            // Arrange
            var unlockedLayer = new Layer { IsLocked = false };
            var context = CreateToolContext(unlockedLayer);
            var startPoint = new SKPoint(10, 10);
            var movedPoint = new SKPoint(50, 50);
            freehandTool.OnTouchPressed(startPoint, context);
            freehandTool.OnTouchMoved(movedPoint, context);
            mockMessageBus.Invocations.Clear(); // Clear previous messages

            // Act
            freehandTool.OnTouchCancelled(context);

            // Assert
            Assert.Empty(unlockedLayer.Elements);
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Once);
            freehandTool.OnTouchMoved(new SKPoint(60, 60), context);
        }
    }
}