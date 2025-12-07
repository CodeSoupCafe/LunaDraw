using System.Collections.ObjectModel;
using Moq;
using Xunit;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using LunaDraw.Logic.Services;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Messages;
using ReactiveUI;

namespace LunaDraw.Tests
{
    public class CanvasInputHandlerRobustTests
    {
        private readonly Mock<IToolStateManager> mockToolStateManager;
        private readonly Mock<ILayerStateManager> mockLayerStateManager;
        private readonly Mock<IMessageBus> mockMessageBus;
        private readonly SelectionManager selectionManager;
        private readonly NavigationModel navigationModel;
        private readonly CanvasInputHandler handler;
        private readonly Mock<IDrawingTool> mockActiveTool;

        public CanvasInputHandlerRobustTests()
        {
            mockToolStateManager = new Mock<IToolStateManager>();
            mockLayerStateManager = new Mock<ILayerStateManager>();
            mockMessageBus = new Mock<IMessageBus>();
            selectionManager = new SelectionManager();
            navigationModel = new NavigationModel();
            mockActiveTool = new Mock<IDrawingTool>();

            // Setup default behavior
            mockToolStateManager.Setup(m => m.ActiveTool).Returns(mockActiveTool.Object);
            mockToolStateManager.Setup(m => m.StrokeColor).Returns(SKColors.Black); // setup some defaults for ToolContext

            mockLayerStateManager.Setup(m => m.CurrentLayer).Returns(new Layer());
            mockLayerStateManager.Setup(m => m.Layers).Returns(new ObservableCollection<Layer>());

            handler = new CanvasInputHandler(
                mockToolStateManager.Object,
                mockLayerStateManager.Object,
                selectionManager,
                navigationModel,
                mockMessageBus.Object
            );
        }

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            Assert.NotNull(_handler);
        }

        [Fact]
        public void ProcessTouch_NoCurrentLayer_DoesNothing()
        {
            // Arrange
            mockLayerStateManager.Setup(m => m.CurrentLayer).Returns((Layer?)null);
            var touch = new SKTouchEventArgs(1, SKTouchAction.Pressed, new SKPoint(10, 10), true);

            // Act
            handler.ProcessTouch(touch, SKRect.Empty);

            // Assert
            mockActiveTool.Verify(t => t.OnTouchPressed(It.IsAny<SKPoint>(), It.IsAny<ToolContext>()), Times.Never);
        }

        [Fact]
        public void ProcessTouch_SingleTouch_Pressed_CallsActiveTool()
        {
            // Arrange
            var touchPoint = new SKPoint(100, 100);
            var touch = new SKTouchEventArgs(1, SKTouchAction.Pressed, touchPoint, true);

            // Act
            handler.ProcessTouch(touch, SKRect.Empty);

            // Assert
            mockActiveTool.Verify(t => t.OnTouchPressed(
                It.Is<SKPoint>(p => p == touchPoint),
                It.IsAny<ToolContext>()
            ), Times.Once);
        }

        [Fact]
        public void ProcessTouch_SingleTouch_Moved_CallsActiveTool()
        {
            // Arrange
            var touchPoint = new SKPoint(100, 100);
            // Must press first to register the touch
            handler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, touchPoint, true), SKRect.Empty);

            var movePoint = new SKPoint(110, 110);
            var touchMove = new SKTouchEventArgs(1, SKTouchAction.Moved, movePoint, true);

            // Act
            handler.ProcessTouch(touchMove, SKRect.Empty);

            // Assert
            mockActiveTool.Verify(t => t.OnTouchMoved(
                It.Is<SKPoint>(p => p == movePoint),
                It.IsAny<ToolContext>()
            ), Times.Once);
        }

        [Fact]
        public void ProcessTouch_SingleTouch_Released_CallsActiveTool()
        {
            // Arrange
            var touchPoint = new SKPoint(100, 100);
            handler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, touchPoint, true), SKRect.Empty);

            var releasePoint = new SKPoint(110, 110);
            var touchRelease = new SKTouchEventArgs(1, SKTouchAction.Released, releasePoint, true);

            // Act
            handler.ProcessTouch(touchRelease, SKRect.Empty);

            // Assert
            mockActiveTool.Verify(t => t.OnTouchReleased(
                It.Is<SKPoint>(p => p == releasePoint),
                It.IsAny<ToolContext>()
            ), Times.Once);
        }

        [Theory]
        [InlineData(ToolType.Freehand, true)]
        [InlineData(ToolType.Select, false)]
        public void ProcessTouch_Pressed_ClearsSelection_DependingOnTool(ToolType toolType, bool shouldClear)
        {
            // Arrange
            mockActiveTool.Setup(t => t.Type).Returns(toolType);

            // Add a mock item to selection
            var mockDrawable = new Mock<IDrawableElement>();
            selectionManager.Add(mockDrawable.Object);
            Assert.True(_selectionManager.HasSelection);

            var touch = new SKTouchEventArgs(1, SKTouchAction.Pressed, new SKPoint(10, 10), true);

            // Act
            handler.ProcessTouch(touch, SKRect.Empty);

            // Assert
            if (shouldClear)
            {
                Assert.False(selectionManager.HasSelection);
                mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.AtLeastOnce);
            }
            else
            {
                Assert.True(selectionManager.HasSelection);
            }
        }

        [Fact]
        public void ProcessTouch_MultiTouch_Start_CancelsActiveTool()
        {
            // Arrange
            // Touch 1
            handler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, new SKPoint(10, 10), true), SKRect.Empty);

            // Act
            // Touch 2 (Triggers multi-touch)
            handler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Pressed, new SKPoint(50, 50), true), SKRect.Empty);

            // Assert
            mockActiveTool.Verify(t => t.OnTouchCancelled(It.IsAny<ToolContext>()), Times.Once);
        }

        [Fact]
        public void ProcessTouch_MultiTouch_PinchZoom_UpdatesUserMatrix()
        {
            // Arrange
            var initialMatrix = navigationModel.UserMatrix;

            // Start with two fingers
            // P1: (0,0)
            // P2: (100,0)
            // Distance: 100
            // Centroid: (50,0)
            handler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, new SKPoint(0, 0), true), SKRect.Empty);
            handler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Pressed, new SKPoint(100, 0), true), SKRect.Empty);

            // Move P2 further out to (200, 0) -> 2x scale
            // Distance: 200
            // Centroid: (100, 0)
            // Since this is the first move, it calculates deltas from the *previous frame* (which was initialized at press)
            // Previous Distance: 100
            // Current Distance: 200 -> Scale Delta: 2.0
            var touchMove = new SKTouchEventArgs(2, SKTouchAction.Moved, new SKPoint(200, 0), true);

            // Act
            handler.ProcessTouch(touchMove, SKRect.Empty);

            // Assert
            var newMatrix = navigationModel.UserMatrix;
            Assert.NotEqual(initialMatrix, newMatrix);
            // Scale should be roughly 2.0
            Assert.True(newMatrix.ScaleX > 1.5f);
        }

        [Fact]
        public void ProcessTouch_MultiTouch_DragSelected_TransformsElement()
        {
            // Arrange
            mockActiveTool.Setup(t => t.Type).Returns(ToolType.Select);

            var mockDrawable = new Mock<IDrawableElement>();
            mockDrawable.SetupProperty(m => m.TransformMatrix, SKMatrix.CreateIdentity());
            mockDrawable.SetupProperty(m => m.IsSelected);
            mockDrawable.Setup(m => m.HitTest(It.IsAny<SKPoint>())).Returns(true); // Always hit

            selectionManager.Add(mockDrawable.Object);

            // Start with two fingers ON the selected element (HitTest returns true)
            handler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, new SKPoint(0, 0), true), SKRect.Empty);
            handler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Pressed, new SKPoint(10, 0), true), SKRect.Empty);

            // Move both fingers by (10, 10) -> Pan
            // P1: (10, 10)
            // P2: (20, 10)
            // Centroid Delta: (10, 10)
            handler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Moved, new SKPoint(10, 10), true), SKRect.Empty);
            handler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Moved, new SKPoint(20, 10), true), SKRect.Empty);

            // Act
            // Note: The handler processes one event at a time. The first move might trigger a small change, 
            // but effectively we want to check if the element's transform matrix changed.

            // Assert
            // Use VerifySet to ensure the property was set with a translation
            mockDrawable.VerifySet(m => m.TransformMatrix = It.Is<SKMatrix>(mat => mat.TransX > 0 && mat.TransY > 0));
        }

        [Fact]
        public void ProcessTouch_MultiTouch_LockedLayer_DoesNotTransformSelection()
        {
            // Arrange
            var mockDrawable = new Mock<IDrawableElement>();
            mockDrawable.SetupProperty(m => m.TransformMatrix, SKMatrix.CreateIdentity());
            mockDrawable.SetupProperty(m => m.IsSelected);
            mockDrawable.Setup(m => m.HitTest(It.IsAny<SKPoint>())).Returns(true);

            selectionManager.Add(mockDrawable.Object);

            // Lock the layer
            var layer = new Layer { IsLocked = true };
            mockLayerStateManager.Setup(m => m.CurrentLayer).Returns(layer);

            // Start touch
            handler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Pressed, new SKPoint(0, 0), true), SKRect.Empty);
            handler.ProcessTouch(new SKTouchEventArgs(2, SKTouchAction.Pressed, new SKPoint(10, 0), true), SKRect.Empty);

            // Move
            handler.ProcessTouch(new SKTouchEventArgs(1, SKTouchAction.Moved, new SKPoint(10, 10), true), SKRect.Empty);

            // Assert
            // Should NOT have transformed because layer is locked
            Assert.Equal(SKMatrix.CreateIdentity(), mockDrawable.Object.TransformMatrix);
        }
    }
}
