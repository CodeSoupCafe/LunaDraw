using System;
using System.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
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
            layers.Should().HaveCount(1);
        }

        [Fact]
        public void Constructor_ShouldSetCurrentLayer()
        {
            // Act
            var currentLayer = layerStateManager.CurrentLayer;

            // Assert
            currentLayer.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldSetCurrentLayerName()
        {
            // Act
            var layerName = layerStateManager.CurrentLayer?.Name;

            // Assert
            layerName.Should().Be("Layer 1");
        }

        [Fact]
        public void AddLayer_ShouldIncreaseLayerCount()
        {
            // Arrange
            // (LayerStateManager initialized in constructor)

            // Act
            layerStateManager.AddLayer();

            // Assert
            layerStateManager.Layers.Should().HaveCount(2);
        }

        [Fact]
        public void AddLayer_ShouldChangeCurrentLayer()
        {
            // Arrange
            var initialLayer = layerStateManager.CurrentLayer;

            // Act
            layerStateManager.AddLayer();

            // Assert
            layerStateManager.CurrentLayer.Should().NotBe(initialLayer);
        }

        [Fact]
        public void AddLayer_ShouldSetNewLayerAsCurrent()
        {
            // Act
            layerStateManager.AddLayer();

            // Assert
            layerStateManager.CurrentLayer!.Name.Should().Be("Layer 2");
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
            layerStateManager.Layers.Should().HaveCount(1);
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
            layerStateManager.CurrentLayer!.Name.Should().Be("Layer 1");
        }

        [Fact]
        public void RemoveLayer_WhenOnlyOneLayer_ShouldNotRemove()
        {
            // Arrange
            var layer1 = layerStateManager.CurrentLayer!;

            // Act
            layerStateManager.RemoveLayer(layer1);

            // Assert
            layerStateManager.Layers.Should().HaveCount(1);
        }

        [Fact]
        public void DrawingStateChangedMessage_ShouldTriggerHistorySave()
        {
            // Arrange
            // HistoryManager saves state in constructor of LayerStateManager.
            // So, CanUndo should be false initially because historyIndex is 0.
            layerStateManager.HistoryManager.CanUndo.Should().BeFalse(); // FIX HERE
            layerStateManager.HistoryManager.CanRedo.Should().BeFalse(); // No redo possible yet

            // Act
            drawingStateSubject.OnNext(new DrawingStateChangedMessage());

            // Assert
            // After another state save, CanUndo should now be true.
            // If there were any redo states, they should be cleared, so CanRedo should be false
            layerStateManager.HistoryManager.CanUndo.Should().BeTrue(); // FIX HERE
            layerStateManager.HistoryManager.CanRedo.Should().BeFalse();
        }
    }
}