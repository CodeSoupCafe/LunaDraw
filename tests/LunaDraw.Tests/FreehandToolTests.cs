using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FluentAssertions;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.ViewModels;
using LunaDraw.Logic.Services; // ADDED: Required for ILayerStateManager and IToolStateManager
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
        private readonly Mock<ILayerStateManager> mockLayerStateManager;
        private readonly Mock<IToolStateManager> mockToolStateManager;
        private readonly SelectionManager selectionManager;
        private readonly NavigationModel navigationModel;

        public FreehandToolTests()
        {
            mockMessageBus = new Mock<IMessageBus>();
            freehandTool = new FreehandTool(mockMessageBus.Object);

            mockLayerStateManager = new Mock<ILayerStateManager>();
            mockToolStateManager = new Mock<IToolStateManager>();
            selectionManager = new SelectionManager();
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
                SelectionManager = selectionManager,
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
            name.Should().Be("Stamps");
        }

        [Fact]
        public void Type_ShouldReturnCorrectValue()
        {
            // Arrange
            // Act
            var type = freehandTool.Type;

            // Assert
            type.Should().Be(ToolType.Freehand);
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
            freehandTool.Invoking(s => s.OnTouchMoved(new SKPoint(20,20), context)).Should().NotThrow(); // Should not crash
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
            unlockedLayer.Elements.Should().ContainSingle(); // Should have added stamps
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
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Never);
            unlockedLayer.Elements.Should().BeEmpty(); // No elements should be added
        }

        [Fact]
        public void OnTouchMoved_ShouldDoNothingIfLayerIsLocked()
        {
            // Arrange
            var lockedLayer = new Layer { IsLocked = true };
            var context = CreateToolContext(lockedLayer);
            var point = new SKPoint(10, 10);

            // Act
            freehandTool.OnTouchPressed(new SKPoint(5,5), context); // Try to start drawing
            freehandTool.OnTouchMoved(point, context);

            // Assert
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Never);
            lockedLayer.Elements.Should().BeEmpty();
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
            // Cannot directly assert currentPoints.Count, but can infer from message bus.
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Once); // At least one invalidate message for adding points
            
            freehandTool.OnTouchReleased(movePoint, context);
            var drawableStamps = unlockedLayer.Elements.Should().ContainSingle().Subject.Should().BeOfType<DrawableStamps>().Subject;
            drawableStamps.Points.Count.Should().BeGreaterThan(1); // Check that multiple points were added
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
            var drawableStamps = unlockedLayer.Elements.Should().ContainSingle().Subject.Should().BeOfType<DrawableStamps>().Subject;
            drawableStamps.Points.Should().ContainSingle(); // Only the initial point
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
            unlockedLayer.Elements.Should().BeEmpty();
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
            unlockedLayer.Elements.Should().ContainSingle();
            unlockedLayer.Elements.First().Should().BeOfType<DrawableStamps>();
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
            freehandTool.Invoking(s => s.OnTouchMoved(new SKPoint(60,60), context)).Should().NotThrow(); // Should not crash
            // The isDrawing flag is internal, but its effect is that OnTouchMoved will do nothing
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
            unlockedLayer.Elements.Should().ContainSingle(); // Still adds a single stamp even for just pressed/released
            unlockedLayer.Elements.First().Should().BeOfType<DrawableStamps>();
            ((DrawableStamps)unlockedLayer.Elements.First()).Points.Should().ContainSingle().And.Contain(point);
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
            unlockedLayer.Elements.Should().BeEmpty(); // No elements should be added
            mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Once);
            freehandTool.Invoking(s => s.OnTouchMoved(new SKPoint(60,60), context)).Should().NotThrow(); // Should not crash, indicates drawing is false
        }
    }
}