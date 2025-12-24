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

using System.Collections.ObjectModel;
using Moq;
using Xunit;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.ViewModels;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using CommunityToolkit.Maui.Storage;

namespace LunaDraw.Tests
{
  public class CanvasInputHandlerRobustTests
  {
    private readonly Mock<ToolbarViewModel> mockToolbarViewModel;
    private readonly Mock<ILayerFacade> mockLayerFacade;
    private readonly Mock<IMessageBus> mockMessageBus;
    private readonly SelectionObserver selectionObserver;
    private readonly NavigationModel navigationModel;
    private readonly CanvasInputHandler handler;
    private readonly Mock<IDrawingTool> mockActiveTool;

    private const float SmoothingFactor = 0.1f; // Matching CanvasInputHandler

    public CanvasInputHandlerRobustTests()
    {
      RxApp.MainThreadScheduler = Scheduler.Immediate;

      mockLayerFacade = new Mock<ILayerFacade>();
      mockMessageBus = new Mock<IMessageBus>();
      selectionObserver = new SelectionObserver();
      navigationModel = new NavigationModel();
      mockActiveTool = new Mock<IDrawingTool>();

      mockLayerFacade.Setup(m => m.Layers).Returns(new ObservableCollection<Layer>());
      mockLayerFacade.Setup(m => m.HistoryMemento).Returns(new HistoryMemento());

      // Create dependencies for ToolbarViewModel
      var selectionVM = new SelectionViewModel(selectionObserver, mockLayerFacade.Object, new ClipboardMemento(), mockMessageBus.Object);
      var historyVM = new HistoryViewModel(mockLayerFacade.Object, mockMessageBus.Object);
      var mockBitmapCache = new Mock<IBitmapCache>();
      var mockFileSaver = new Mock<IFileSaver>();
      var mockPreferences = new Mock<IPreferencesFacade>();

      // Ensure MessageBus returns observables for ToolbarViewModel constructor
      mockMessageBus.Setup(x => x.Listen<BrushSettingsChangedMessage>()).Returns(Observable.Empty<BrushSettingsChangedMessage>());
      mockMessageBus.Setup(x => x.Listen<BrushShapeChangedMessage>()).Returns(Observable.Empty<BrushShapeChangedMessage>());
      mockMessageBus.Setup(x => x.Listen<ViewOptionsChangedMessage>()).Returns(Observable.Empty<ViewOptionsChangedMessage>());

      mockToolbarViewModel = new Mock<ToolbarViewModel>(
          mockLayerFacade.Object,
          selectionVM,
          historyVM,
          mockMessageBus.Object,
          mockBitmapCache.Object,
          navigationModel,
          mockFileSaver.Object,
          mockPreferences.Object
      );

      // Setup default behavior
      mockToolbarViewModel.Setup(m => m.ActiveTool).Returns(mockActiveTool.Object);
      mockToolbarViewModel.Setup(m => m.StrokeColor).Returns(SKColors.Black); // setup some defaults for ToolContext

      mockLayerFacade.Setup(m => m.CurrentLayer).Returns(new Layer());

      var mockPlaybackHandler = new Moq.Mock<LunaDraw.Logic.Handlers.IPlaybackHandler>();
      handler = new CanvasInputHandler(
          mockToolbarViewModel.Object,
          mockLayerFacade.Object,
          selectionObserver,
          navigationModel,
          mockPlaybackHandler.Object,
          mockMessageBus.Object
      );
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
      Assert.NotNull(handler);
    }

    [Fact]
    public void ProcessTouch_NoCurrentLayer_DoesNothing()
    {
      // Arrange
      mockLayerFacade.Setup(m => m.CurrentLayer).Returns((Layer?)null);
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
      selectionObserver.Add(mockDrawable.Object);
      Assert.True(selectionObserver.HasSelection);

      var touch = new SKTouchEventArgs(1, SKTouchAction.Pressed, new SKPoint(10, 10), true);

      // Act
      handler.ProcessTouch(touch, SKRect.Empty);

      // Assert
      if (shouldClear)
      {
        Assert.False(selectionObserver.HasSelection);
        mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.AtLeastOnce);
      }
      else
      {
        Assert.True(selectionObserver.HasSelection);
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
      var initialMatrix = navigationModel.ViewMatrix;

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
      var newMatrix = navigationModel.ViewMatrix;
      Assert.NotEqual(initialMatrix, newMatrix);
      // Scale should be roughly 2.0
      Assert.True(Math.Abs(navigationModel.ViewMatrix.ScaleX - (1 + (2.0f - 1) * SmoothingFactor)) < 0.1f);
      Assert.True(Math.Abs(navigationModel.ViewMatrix.ScaleY - (1 + (2.0f - 1) * SmoothingFactor)) < 0.1f);
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

      selectionObserver.Add(mockDrawable.Object);

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

      selectionObserver.Add(mockDrawable.Object);

      // Lock the layer
      var layer = new Layer { IsLocked = true };
      mockLayerFacade.Setup(m => m.CurrentLayer).Returns(layer);

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
