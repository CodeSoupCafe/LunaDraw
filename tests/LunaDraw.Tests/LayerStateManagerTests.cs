using System;
using System.Linq;
using System.Reactive.Subjects;

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Services; // Keep this for LayerStateManager
using Moq;
using Xunit;
using ReactiveUI; // ADDED: Required for IMessageBus

namespace LunaDraw.Tests
{
    public class LayerStateManagerTests
    {
        private readonly Mock<IMessageBus> mockBus;
        private readonly Subject<DrawingStateChangedMessage> drawingStateSubject;
        private readonly LayerStateManager layerStateManager;

        public LayerStateManagerTests()
        {
            mockBus = new Mock<IMessageBus>();
            drawingStateSubject = new Subject<DrawingStateChangedMessage>();

            mockBus.Setup(x => x.Listen<DrawingStateChangedMessage>())
                .Returns(drawingStateSubject);

            layerStateManager = new LayerStateManager(mockBus.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeWithOneLayer()
        {
            // Act
            var layers = layerStateManager.Layers;

            // Assert
            Assert.Single(layers);
        }

        [Fact]
        public void Constructor_ShouldSetCurrentLayer()
        {
            // Act
            var currentLayer = layerStateManager.CurrentLayer;

            // Assert
            Assert.NotNull(currentLayer);
        }

        [Fact]
        public void Constructor_ShouldSetCurrentLayerName()
        {
            // Act
            var layerName = layerStateManager.CurrentLayer?.Name;

            // Assert
            Assert.Equal("Layer 1", layerName);
        }

        [Fact]
        public void AddLayer_ShouldIncreaseLayerCount()
        {
            // Arrange
            // (LayerStateManager initialized in constructor)

            // Act
            layerStateManager.AddLayer();

            // Assert
            Assert.Equal(2, layerStateManager.Layers.Count);
        }

        [Fact]
        public void AddLayer_ShouldChangeCurrentLayer()
        {
            // Arrange
            var initialLayer = layerStateManager.CurrentLayer;

            // Act
            layerStateManager.AddLayer();

            // Assert
            Assert.NotEqual(initialLayer, layerStateManager.CurrentLayer);
        }

        [Fact]
        public void AddLayer_ShouldSetNewLayerAsCurrent()
        {
            // Act
            layerStateManager.AddLayer();

            // Assert
            Assert.Equal("Layer 2", layerStateManager.CurrentLayer!.Name);
        }

        [Fact]
        public void RemoveLayer_ShouldDecreaseLayerCount()
        {
            // Arrange
            layerStateManager.AddLayer();
            var layerToRemove = layerStateManager.CurrentLayer!;

            // Act
            layerStateManager.RemoveLayer(layerToRemove);

            // Assert
            Assert.Single(layerStateManager.Layers);
        }

        [Fact]
        public void RemoveLayer_ShouldSetFirstLayerAsCurrent()
        {
            // Arrange
            layerStateManager.AddLayer(); // Layers: L1, L2(current)
            var layerToRemove = layerStateManager.CurrentLayer!;

            // Act
            layerStateManager.RemoveLayer(layerToRemove);

            // Assert
            Assert.Equal("Layer 1", layerStateManager.CurrentLayer!.Name);
        }

        [Fact]
        public void RemoveLayer_WhenOnlyOneLayer_ShouldNotRemove()
        {
            // Arrange
            var layer1 = layerStateManager.CurrentLayer!;

            // Act
            layerStateManager.RemoveLayer(layer1);

            // Assert
            Assert.Single(layerStateManager.Layers);
        }

        [Fact]
        public void DrawingStateChangedMessage_ShouldTriggerHistorySave()
        {
            // Arrange
            // HistoryManager saves state in constructor of LayerStateManager.
            // So, CanUndo should be false initially because historyIndex is 0.
            Assert.False(layerStateManager.HistoryManager.CanUndo); // FIX HERE
            Assert.False(layerStateManager.HistoryManager.CanRedo); // No redo possible yet

            // Act
            drawingStateSubject.OnNext(new DrawingStateChangedMessage());

            // Assert
            // After another state save, CanUndo should now be true.
            // If there were any redo states, they should be cleared, so CanRedo should be false
            Assert.True(layerStateManager.HistoryManager.CanUndo); // FIX HERE
            Assert.False(layerStateManager.HistoryManager.CanRedo);
        }
    }
}