using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;
using Moq;
using ReactiveUI;
using SkiaSharp;
using Xunit;

namespace LunaDraw.Tests
{
    public class SelectionViewModelTests
    {
        private readonly Mock<IMessageBus> mockBus;
        private readonly LayerStateManager layerStateManager;
        private readonly SelectionManager selectionManager;
        private readonly ClipboardManager clipboardManager;
        private readonly SelectionViewModel viewModel;

        public SelectionViewModelTests()
        {
            mockBus = new Mock<IMessageBus>();
            // Setup generic listeners to avoid null reference exceptions
            mockBus.Setup(x => x.Listen<DrawingStateChangedMessage>()).Returns(Observable.Empty<DrawingStateChangedMessage>());
            mockBus.Setup(x => x.Listen<SelectionChangedMessage>()).Returns(Observable.Empty<SelectionChangedMessage>());

            layerStateManager = new LayerStateManager(mockBus.Object);
            selectionManager = new SelectionManager();
            clipboardManager = new ClipboardManager();

            viewModel = new SelectionViewModel(selectionManager, layerStateManager, clipboardManager, mockBus.Object);
        }

        [Fact]
        public void MoveSelectionToNewLayer_ShouldCreateLayerAndMoveElements()
        {
            // Arrange
            var layer1 = layerStateManager.Layers.First();
            var element = new DrawableRectangle { Rectangle = new SKRect(0, 0, 10, 10) };
            layer1.Elements.Add(element);

            selectionManager.Add(element);

            Assert.Equal(1, layerStateManager.Layers.Count);
            Assert.Contains(element, layer1.Elements);

            // Act
            viewModel.MoveSelectionToNewLayerCommand.Execute().Subscribe();

            // Assert
            Assert.Equal(2, layerStateManager.Layers.Count);
            var layer2 = layerStateManager.Layers[1];
            
            // Element should be in Layer 2
            Assert.Contains(element, layer2.Elements);
            // Element should NOT be in Layer 1
            Assert.DoesNotContain(element, layer1.Elements);
        }
    }
}
