using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Moq;
using Xunit;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using ReactiveUI;
using LunaDraw.Logic.Services;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.ViewModels;
using CommunityToolkit.Maui.Storage;

namespace LunaDraw.Tests
{
    public class CanvasInputHandlerTests
    {
        private readonly ToolbarViewModel toolbarViewModel;
        private readonly Mock<ILayerFacade> mockLayerFacade;
        private readonly Mock<IMessageBus> mockMessageBus;
        private readonly Mock<IDrawingTool> mockDrawingTool;
        private readonly Mock<IBitmapCache> mockBitmapCache;
        private readonly Mock<IFileSaver> mockFileSaver;
        private readonly SelectionObserver selectionObserver;
        private readonly NavigationModel navigationModel;
        private readonly CanvasInputHandler canvasInputHandler;

        private const float SmoothingFactor = 0.1f; 

        public CanvasInputHandlerTests()
        {
            mockLayerFacade = new Mock<ILayerFacade>();
            mockMessageBus = new Mock<IMessageBus>();
            mockDrawingTool = new Mock<IDrawingTool>();
            mockBitmapCache = new Mock<IBitmapCache>();
            mockFileSaver = new Mock<IFileSaver>();

            mockLayerFacade.Setup(m => m.CurrentLayer).Returns(new Layer());
            mockLayerFacade.Setup(m => m.Layers).Returns(new ObservableCollection<Layer>());
            mockDrawingTool.Setup(t => t.Type).Returns(ToolType.Freehand);

            selectionObserver = new SelectionObserver();
            navigationModel = new NavigationModel();

            // Instantiate real ViewModels
            var clipboardMemento = new ClipboardMemento();
            var selectionVM = new SelectionViewModel(selectionObserver, mockLayerFacade.Object, clipboardMemento, mockMessageBus.Object);
            var historyVM = new HistoryViewModel(mockLayerFacade.Object, mockMessageBus.Object);

            toolbarViewModel = new ToolbarViewModel(
                mockLayerFacade.Object,
                selectionVM,
                historyVM,
                mockMessageBus.Object,
                mockBitmapCache.Object,
                navigationModel,
                mockFileSaver.Object
            );

            // Inject mock tool
            toolbarViewModel.ActiveTool = mockDrawingTool.Object;

            canvasInputHandler = new CanvasInputHandler(
                toolbarViewModel,
                mockLayerFacade.Object,
                selectionObserver,
                navigationModel,
                mockMessageBus.Object
            );
        }

        [Fact]
        public void HandleMultiTouch_MissingTouchKey_IgnoresTouch()
        {
            // Arrange
            // Create a local handler with clean state if needed, or use the class one.
            // The original test created a new one. We can do the same.
            
            var clipboardMemento = new ClipboardMemento();
            var localSelectionVM = new SelectionViewModel(selectionObserver, mockLayerFacade.Object, clipboardMemento, mockMessageBus.Object);
            var localHistoryVM = new HistoryViewModel(mockLayerFacade.Object, mockMessageBus.Object);
            
            var localToolbarVM = new ToolbarViewModel(
                mockLayerFacade.Object,
                localSelectionVM,
                localHistoryVM,
                mockMessageBus.Object,
                mockBitmapCache.Object,
                navigationModel,
                mockFileSaver.Object
            );
            
            // Mock active tool inside the local VM
            localToolbarVM.ActiveTool = new Mock<IDrawingTool>().Object;

            var handler = new CanvasInputHandler(
                localToolbarVM,
                mockLayerFacade.Object,
                selectionObserver,
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
            canvasInputHandler.ProcessTouch(eventArgsPressed, SKRect.Empty); 

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
            canvasInputHandler.ProcessTouch(eventArgsPressed, SKRect.Empty); 

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
            mockLayerFacade.Setup(m => m.CurrentLayer).Returns(default(Layer));
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
            canvasInputHandler.ProcessTouch(touch1, SKRect.Empty); 

            var touch2 = new SKTouchEventArgs(2, SKTouchAction.Pressed, new SKPoint(20, 20), true);

            // Act
            canvasInputHandler.ProcessTouch(touch2, SKRect.Empty); 

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
            var touch1Move = new SKPoint(110, 110); 
            var touch2Move = new SKPoint(210, 110); 

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
            var touch2Start = new SKPoint(200, 100); 

            // Simulate two fingers pressed
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, touch1Start, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Pressed, touch2Start, true), SKRect.Empty);

            // Simulate pinch out (increase distance)
            var touch1Move = new SKPoint(75, 100); 
            var touch2Move = new SKPoint(225, 100); 

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

            // Simulate rotation
            var touch1Move = new SKPoint(100, 50);
            var touch2Move = new SKPoint(200, 150);

            // Act
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Moved, touch1Move, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Moved, touch2Move, true), SKRect.Empty);

            // Assert
            var finalMatrix = navigationModel.ViewMatrix;
            Assert.NotEqual(initialMatrix, finalMatrix);
            Assert.True(Math.Abs(finalMatrix.ScaleX - initialMatrix.ScaleX) < 0.1f);
        }

        [Fact]
        public void ProcessTouch_TwoFingersPanSelectedElements_ShouldUpdateElementTransformMatrix()
        {
            // Arrange
            mockDrawingTool.Setup(t => t.Type).Returns(ToolType.Select);
            mockLayerFacade.Setup(m => m.CurrentLayer).Returns(new Layer { IsLocked = false });

            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupProperty(e => e.TransformMatrix); 
            mockElement.Object.TransformMatrix = SKMatrix.CreateIdentity(); 
            mockElement.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(true); 
            selectionObserver.Add(mockElement.Object); 

            navigationModel.ViewMatrix = SKMatrix.CreateIdentity();
            navigationModel.ViewMatrix = SKMatrix.CreateIdentity(); 

            var touch1Start = new SKPoint(100, 100);
            var touch2Start = new SKPoint(200, 100);

            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, touch1Start, true), SKRect.Empty);
            canvasInputHandler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Pressed, touch2Start, true), SKRect.Empty);

            var initialElementMatrix = mockElement.Object.TransformMatrix;

            // Move both fingers in parallel
            var touch1Move = new SKPoint(110, 110); 
            var touch2Move = new SKPoint(210, 110); 

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
            mockLayerFacade.Setup(m => m.CurrentLayer).Returns(new Layer { IsLocked = false });

            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupProperty(e => e.TransformMatrix); 
            mockElement.Object.TransformMatrix = SKMatrix.CreateIdentity(); 
            mockElement.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(true);
            selectionObserver.Add(mockElement.Object); 

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
            mockLayerFacade.Setup(m => m.CurrentLayer).Returns(new Layer { IsLocked = false });

            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupProperty(e => e.TransformMatrix); 
            mockElement.Object.TransformMatrix = SKMatrix.CreateIdentity(); 
            mockElement.Setup(e => e.HitTest(It.IsAny<SKPoint>())).Returns(true);
            selectionObserver.Add(mockElement.Object); 

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