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
using Xunit;

namespace LunaDraw.Tests
{
    public class LayerPanelViewModelTests
    {
        private readonly Mock<IMessageBus> mockBus;
        private readonly LayerStateManager layerStateManager;
        private readonly LayerPanelViewModel viewModel;
        private readonly Subject<DrawingStateChangedMessage> drawingStateSubject;

        public LayerPanelViewModelTests()
        {
            mockBus = new Mock<IMessageBus>();
            drawingStateSubject = new Subject<DrawingStateChangedMessage>();
            mockBus.Setup(x => x.Listen<DrawingStateChangedMessage>()).Returns(drawingStateSubject);

            layerStateManager = new LayerStateManager(mockBus.Object);
            viewModel = new LayerPanelViewModel(layerStateManager, mockBus.Object);
        }

        [Fact]
        public void AddLayer_ShouldUpdateCurrentLayerInViewModel()
        {
            // Act
            viewModel.AddLayerCommand.Execute().Subscribe();

            // Assert
            Assert.Equal(2, layerStateManager.Layers.Count);
            Assert.Equal("Layer 2", viewModel.CurrentLayer?.Name);
        }

        [Fact]
        public void RemoveLayer_ShouldUseCorrectCurrentLayer_AfterAdd()
        {
            // Arrange
            viewModel.AddLayerCommand.Execute().Subscribe(); // Adds Layer 2, sets as Current

            Assert.Equal("Layer 2", viewModel.CurrentLayer?.Name);

            // Act
            // Execute parameterless command
            viewModel.RemoveLayerCommand.Execute().Subscribe();

            // Assert
            Assert.Single(layerStateManager.Layers);
            Assert.Equal("Layer 1", viewModel.CurrentLayer?.Name);
        }
        
        [Fact]
        public void RemoveLayer_ShouldBeDisabled_WhenOneLayer()
        {
             // Arrange
             // Only Layer 1 exists initially

             // Act
             bool canExecute = viewModel.RemoveLayerCommand.CanExecute.FirstAsync().Wait();

             // Assert
             Assert.False(canExecute);
        }

        [Fact]
        public void RemoveLayer_ShouldBeEnabled_WhenTwoLayers()
        {
             // Arrange
             viewModel.AddLayerCommand.Execute().Subscribe();

             // Act
             bool canExecute = viewModel.RemoveLayerCommand.CanExecute.FirstAsync().Wait();

             // Assert
             Assert.True(canExecute);
        }
    }
}
