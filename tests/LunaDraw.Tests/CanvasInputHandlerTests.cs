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
        [Fact]
        public void HandleMultiTouch_MissingTouchKey_IgnoresTouch()
        {
            // Arrange
            var mockToolStateManager = new Mock<IToolStateManager>();
            var mockLayerStateManager = new Mock<ILayerStateManager>();
            var mockMessageBus = new Mock<IMessageBus>();

            // Setup minimal behavior
            mockToolStateManager.Setup(m => m.ActiveTool).Returns(new Mock<IDrawingTool>().Object);
            
            // Setup CurrentLayer to return a layer so we don't crash on null access if handler checks it
            mockLayerStateManager.Setup(m => m.CurrentLayer).Returns(new Layer());
            mockLayerStateManager.Setup(m => m.Layers).Returns(new ObservableCollection<Layer>());

            var selectionManager = new SelectionManager();
            var navigationModel = new NavigationModel();

            var handler = new CanvasInputHandler(
                mockToolStateManager.Object,
                mockLayerStateManager.Object,
                selectionManager,
                navigationModel,
                mockMessageBus.Object
            );

            // Simulate two touches being pressed (ID 1 and 2)
            var touch1 = new SKTouchEventArgs(1, SKTouchAction.Pressed, new SKPoint(10, 10), true);
            handler.ProcessTouch(touch1, SKRect.Empty, null);

            var touch2 = new SKTouchEventArgs(2, SKTouchAction.Pressed, new SKPoint(20, 20), true);
            handler.ProcessTouch(touch2, SKRect.Empty, null);

            // Simulate a moved event for a third touch (ID 3) that was never pressed
            var touch3 = new SKTouchEventArgs(3, SKTouchAction.Moved, new SKPoint(30, 30), true);

            // Act & Assert
            var exception = Record.Exception(() => handler.ProcessTouch(touch3, SKRect.Empty, null));
            Assert.Null(exception);
        }
    }
}