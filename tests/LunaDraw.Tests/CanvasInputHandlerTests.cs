using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Moq;
using Xunit;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using ReactiveUI;
using LunaDraw.Logic.Services;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;


namespace LunaDraw.Tests
{
    public class CanvasInputHandlerTests
    {
        private readonly Mock<IToolStateManager> mockToolStateManager;
        private readonly Mock<ILayerStateManager> mockLayerStateManager;
        private readonly Mock<IMessageBus> mockMessageBus;
        private readonly Mock<IDrawingTool> mockDrawingTool;
        private readonly SelectionManager selectionManager;
        private readonly NavigationModel navigationModel;
        private readonly CanvasInputHandler canvasInputHandler;

        private const float SmoothingFactor = 0.1f; // Matching CanvasInputHandler

        public CanvasInputHandlerTests()
        {
            mockToolStateManager = new Mock<IToolStateManager>();
            mockLayerStateManager = new Mock<ILayerStateManager>();
            mockMessageBus = new Mock<IMessageBus>();
            mockDrawingTool = new Mock<IDrawingTool>();

            mockToolStateManager.Setup(m => m.ActiveTool).Returns(mockDrawingTool.Object);
            mockLayerStateManager.Setup(m => m.CurrentLayer).Returns(new Layer());
            mockLayerStateManager.Setup(m => m.Layers).Returns(new ObservableCollection<Layer>());
            mockDrawingTool.Setup(t => t.Type).Returns(ToolType.Freehand); // Default for drawing tools

            selectionManager = new SelectionManager();
            navigationModel = new NavigationModel();

            canvasInputHandler = new CanvasInputHandler(
                mockToolStateManager.Object,
                mockLayerStateManager.Object,
                selectionManager,
                navigationModel,
                mockMessageBus.Object
            );
        }

        [Fact]
        public void HandleMultiTouch_MissingTouchKey_IgnoresTouch()
        {
            // Arrange
            // Setup for this specific test
            mockToolStateManager.Setup(m => m.ActiveTool).Returns(new Mock<IDrawingTool>().Object);
            mockLayerStateManager.Setup(m => m.CurrentLayer).Returns(new Layer());
            mockLayerStateManager.Setup(m => m.Layers).Returns(new ObservableCollection<Layer>());
            
            var handler = new CanvasInputHandler(
                mockToolStateManager.Object,
                mockLayerStateManager.Object,
                selectionManager,
                navigationModel,
                mockMessageBus.Object
            );

            var touch1 = new SKTouchEventArgs(1, SKTouchAction.Pressed, new SKPoint(10, 10), true);
            handler.ProcessTouch(touch1, SKRect.Empty);

            var touch2 = new SKTouchEventArgs(2, SKTouchAction.Pressed, new SKPoint(20, 20), true);
            handler.ProcessTouch(touch2, SKRect.Empty);

            var touch3 = new SKTouchEventArgs(3, SKTouchAction.Moved, new SKPoint(30, 30), true);

            // Act
            var exception = Record.Exception(() => handler.ProcessTouch(touch3, SKRect.Empty));
            
            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void ProcessTouch_Pressed_ShouldCallActiveToolOnTouchPressed()
        {
            // Arrange
            var touchLocation = new SKPoint(100, 100);
            var eventArgs = new SKTouchEventArgs(1, SKTouchAction.Pressed, touchLocation, true);
            
            // Act
            canvasInputHandler.ProcessTouch(eventArgs, SKRect.Empty);

            // Assert
            mockDrawingTool.Verify(x => x.OnTouchPressed(It.IsAny<SKPoint>(), It.IsAny<ToolContext>()), Times.Once);
        }

        [Fact]
        public void ProcessTouch_Moved_ShouldCallActiveToolOnTouchMoved()
        {
            // Arrange
            var touchLocation = new SKPoint(100, 100);
            var eventArgsPressed = new SKTouchEventArgs(1, SKTouchAction.Pressed, touchLocation, true);
            canvasInputHandler.ProcessTouch(eventArgsPressed, SKRect.Empty); // Simulate initial press

            var newTouchLocation = new SKPoint(110, 110);
            var eventArgsMoved = new SKTouchEventArgs(1, SKTouchAction.Moved, newTouchLocation, true);
            
            // Act
            canvasInputHandler.ProcessTouch(eventArgsMoved, SKRect.Empty);

            // Assert
            mockDrawingTool.Verify(x => x.OnTouchMoved(It.IsAny<SKPoint>(), It.IsAny<ToolContext>()), Times.Once);
        }

        [Fact]
        public void ProcessTouch_Released_ShouldCallActiveToolOnTouchReleased()
        {
            // Arrange
            var touchLocation = new SKPoint(100, 100);
            var eventArgsPressed = new SKTouchEventArgs(1, SKTouchAction.Pressed, touchLocation, true);
            canvasInputHandler.ProcessTouch(eventArgsPressed, SKRect.Empty); // Simulate initial press

            var eventArgsReleased = new SKTouchEventArgs(1, SKTouchAction.Released, touchLocation, true);
            
            // Act
            canvasInputHandler.ProcessTouch(eventArgsReleased, SKRect.Empty);

            // Assert
            mockDrawingTool.Verify(x => x.OnTouchReleased(It.IsAny<SKPoint>(), It.IsAny<ToolContext>()), Times.Once);
        }

        [Fact]
        public void ProcessTouch_WhenLayerIsNull_ShouldNotProcessTouch()
        {
            // Arrange
            mockLayerStateManager.Setup(m => m.CurrentLayer).Returns(default(Layer));
            var touchLocation = new SKPoint(100, 100);
            var eventArgs = new SKTouchEventArgs(1, SKTouchAction.Pressed, touchLocation, true);
            
            // Act
            canvasInputHandler.ProcessTouch(eventArgs, SKRect.Empty);

            // Assert
            mockDrawingTool.Verify(x => x.OnTouchPressed(It.IsAny<SKPoint>(), It.IsAny<ToolContext>()), Times.Never);
            mockDrawingTool.Verify(x => x.OnTouchMoved(It.IsAny<SKPoint>(), It.IsAny<ToolContext>()), Times.Never);
            mockDrawingTool.Verify(x => x.OnTouchReleased(It.IsAny<SKPoint>(), It.IsAny<ToolContext>()), Times.Never);
        }

        [Fact]
        public void ProcessTouch_MultiTouchStarts_ShouldCallActiveToolOnTouchCancelled()
        {
            // Arrange
            var touch1 = new SKTouchEventArgs(1, SKTouchAction.Pressed, new SKPoint(10, 10), true);
            canvasInputHandler.ProcessTouch(touch1, SKRect.Empty); // First touch starts single touch mode

            var touch2 = new SKTouchEventArgs(2, SKTouchAction.Pressed, new SKPoint(20, 20), true);
            
            // Act
            canvasInputHandler.ProcessTouch(touch2, SKRect.Empty); // Second touch initiates multi-touch

            // Assert
            mockDrawingTool.Verify(x => x.OnTouchCancelled(It.IsAny<ToolContext>()), Times.Once);
        }

        [Fact]
        public void ProcessTouch_TwoFingersPan_ShouldUpdateNavigationModelUserMatrixTranslation()
        {
            // Arrange
            var initialMatrix = navigationModel.ViewMatrix;
            var touch1Start = new SKPoint(100, 100);
            var touch2Start = new SKPoint(200, 100);

            // Simulate two fingers pressed
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, touch1Start, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Pressed, touch2Start, true), SKRect.Empty);

            // Move both fingers in parallel
            var touch1Move = new SKPoint(110, 110); // Move +10, +10
            var touch2Move = new SKPoint(210, 110); // Move +10, +10

            // Act
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Moved, touch1Move, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Moved, touch2Move, true), SKRect.Empty);

            // Assert
            var finalMatrix = navigationModel.ViewMatrix;
Assert.True(Math.Abs(finalMatrix.TransX - 2.7252297f) < 0.001f);
            Assert.True(Math.Abs(finalMatrix.TransY - 2.3001537f) < 0.001f);
        }

        [Fact]
        public void ProcessTouch_TwoFingersPinchZoom_ShouldUpdateNavigationModelUserMatrixScale()
        {
            // Arrange
            var initialMatrix = navigationModel.ViewMatrix;
            var touch1Start = new SKPoint(100, 100);
            var touch2Start = new SKPoint(200, 100); // Distance = 100
            
            // Simulate two fingers pressed
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, touch1Start, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Pressed, touch2Start, true), SKRect.Empty);

            // Simulate pinch out (increase distance)
            // Centroid (150, 100) stays same
            var touch1Move = new SKPoint(75, 100); // Moved left by 25
            var touch2Move = new SKPoint(225, 100); // Moved right by 25. Distance = 150 (1.5x original)

            // Act
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Moved, touch1Move, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Moved, touch2Move, true), SKRect.Empty);

            // Assert
            var finalMatrix = navigationModel.ViewMatrix;
            Assert.True(Math.Abs(finalMatrix.ScaleX - (initialMatrix.ScaleX + (initialMatrix.ScaleX * 0.5f * SmoothingFactor))) < 0.1f);
            Assert.True(Math.Abs(finalMatrix.ScaleY - (initialMatrix.ScaleY + (initialMatrix.ScaleY * 0.5f * SmoothingFactor))) < 0.1f);
        }

        [Fact]
        public void ProcessTouch_TwoFingersRotate_ShouldUpdateNavigationModelUserMatrixRotation()
        {
            // Arrange
            var initialMatrix = navigationModel.ViewMatrix;
            var touch1Start = new SKPoint(100, 100);
            var touch2Start = new SKPoint(200, 100); 

            // Simulate two fingers pressed
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, touch1Start, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Pressed, touch2Start, true), SKRect.Empty);

            // Simulate rotation: touch1 moves up, touch2 moves down, maintaining same horizontal distance from center.
            // Centroid (150, 100)
            var touch1Move = new SKPoint(100, 50); 
            var touch2Move = new SKPoint(200, 150); 

            // Act
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Moved, touch1Move, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Moved, touch2Move, true), SKRect.Empty);

            // Assert
            var finalMatrix = navigationModel.ViewMatrix;
            // Verify that the matrix has changed
            Assert.NotEqual(initialMatrix, finalMatrix);
            // Verify that scale is approximately the same (no zoom)
            Assert.True(Math.Abs(finalMatrix.ScaleX - initialMatrix.ScaleX) < 0.1f);
        }
        
        [Fact]
        public void ProcessTouch_TwoFingersPanSelectedElements_ShouldUpdateElementTransformMatrix()
        {
            // Arrange
            // Ensure active tool is Select so it doesn't clear selection on first touch
            mockDrawingTool.Setup(t => t.Type).Returns(ToolType.Select);

            // Ensure CurrentLayer is not locked
            mockLayerStateManager.Setup(m => m.CurrentLayer).Returns(new Layer { IsLocked = false });

            // Create and select a mock drawable element
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupProperty(e => e.TransformMatrix); // FIX HERE: Setup tracking for TransformMatrix
            mockElement.Object.TransformMatrix = SKMatrix.CreateIdentity(); // Initialize after setup
            mockElement.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(true); // Element is always hit
            selectionManager.Add(mockElement.Object); // FIX HERE: Changed from selectionManager.Selected.Add

            // Initialize navigationModel to an identity matrix for simpler verification
            navigationModel.ViewMatrix = SKMatrix.CreateIdentity();
            navigationModel.ViewMatrix = SKMatrix.CreateIdentity(); // Needed for inverse calculation

            var touch1Start = new SKPoint(100, 100);
            var touch2Start = new SKPoint(200, 100);

            // Simulate two fingers pressed to start multi-touch on selected elements
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, touch1Start, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Pressed, touch2Start, true), SKRect.Empty);
            
            var initialElementMatrix = mockElement.Object.TransformMatrix;

            // Move both fingers in parallel
            var touch1Move = new SKPoint(110, 110); // Move +10, +10
            var touch2Move = new SKPoint(210, 110); // Move +10, +10

            // Act
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Moved, touch1Move, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Moved, touch2Move, true), SKRect.Empty);

            // Assert
            var finalElementMatrix = mockElement.Object.TransformMatrix;
            Assert.True(Math.Abs(finalElementMatrix.TransX - 2.7252297f) < 0.001f);
            Assert.True(Math.Abs(finalElementMatrix.TransY - 2.3001537f) < 0.001f);
        }

        [Fact]
        public void ProcessTouch_TwoFingersPinchZoomSelectedElements_ShouldUpdateElementTransformMatrix()
        {
            // Arrange
            mockDrawingTool.Setup(t => t.Type).Returns(ToolType.Select);
            mockLayerStateManager.Setup(m => m.CurrentLayer).Returns(new Layer { IsLocked = false });

            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupProperty(e => e.TransformMatrix); // FIX HERE: Setup tracking for TransformMatrix
            mockElement.Object.TransformMatrix = SKMatrix.CreateIdentity(); // Initialize after setup
            mockElement.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(true);
            selectionManager.Add(mockElement.Object); // FIX HERE: Changed from selectionManager.Selected.Add

            navigationModel.ViewMatrix = SKMatrix.CreateIdentity();
            navigationModel.ViewMatrix = SKMatrix.CreateIdentity();

            var touch1Start = new SKPoint(100, 100);
            var touch2Start = new SKPoint(200, 100); 

            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, touch1Start, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Pressed, touch2Start, true), SKRect.Empty);
            
            var initialElementMatrix = mockElement.Object.TransformMatrix;

            // Simulate pinch out (increase distance)
            var touch1Move = new SKPoint(75, 100); 
            var touch2Move = new SKPoint(225, 100); 

            // Act
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Moved, touch1Move, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Moved, touch2Move, true), SKRect.Empty);

            // Assert
            var finalElementMatrix = mockElement.Object.TransformMatrix;
            Assert.True(Math.Abs(finalElementMatrix.ScaleX - (initialElementMatrix.ScaleX + (initialElementMatrix.ScaleX * 0.5f * SmoothingFactor))) < 0.1f);
            Assert.True(Math.Abs(finalElementMatrix.ScaleY - (initialElementMatrix.ScaleY + (initialElementMatrix.ScaleY * 0.5f * SmoothingFactor))) < 0.1f);
        }

        [Fact]
        public void ProcessTouch_TwoFingersRotateSelectedElements_ShouldUpdateElementTransformMatrix()
        {
            // Arrange
            mockDrawingTool.Setup(t => t.Type).Returns(ToolType.Select);
            mockLayerStateManager.Setup(m => m.CurrentLayer).Returns(new Layer { IsLocked = false });

            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupProperty(e => e.TransformMatrix); // FIX HERE: Setup tracking for TransformMatrix
            mockElement.Object.TransformMatrix = SKMatrix.CreateIdentity(); // Initialize after setup
            mockElement.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(true);
            selectionManager.Add(mockElement.Object); // FIX HERE: Changed from selectionManager.Selected.Add

            navigationModel.ViewMatrix = SKMatrix.CreateIdentity();
            navigationModel.ViewMatrix = SKMatrix.CreateIdentity();

            var touch1Start = new SKPoint(100, 100);
            var touch2Start = new SKPoint(200, 100); 

            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, touch1Start, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Pressed, touch2Start, true), SKRect.Empty);
            
            var initialElementMatrix = mockElement.Object.TransformMatrix;

            // Simulate rotation
            var touch1Move = new SKPoint(100, 50); 
            var touch2Move = new SKPoint(200, 150); 

            // Act
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Moved, touch1Move, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Moved, touch2Move, true), SKRect.Empty);

            // Assert
            var finalElementMatrix = mockElement.Object.TransformMatrix;
            Assert.NotEqual(initialElementMatrix, finalElementMatrix);
            Assert.True(Math.Abs(finalElementMatrix.ScaleX - initialElementMatrix.ScaleX) < 0.1f);
            Assert.True(Math.Abs(finalElementMatrix.ScaleY - initialElementMatrix.ScaleY) < 0.1f);

        }
    }
}