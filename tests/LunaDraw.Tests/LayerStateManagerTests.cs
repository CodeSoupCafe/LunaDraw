using System;
using System.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
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
        private readonly LayerStateManager sut;

        public LayerStateManagerTests()
        {
            mockBus = new Mock<IMessageBus>();
            drawingStateSubject = new Subject<DrawingStateChangedMessage>();

            mockBus.Setup(x => x.Listen<DrawingStateChangedMessage>())
                .Returns(drawingStateSubject);

            sut = new LayerStateManager(mockBus.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeWithOneLayer()
        {
            // Act
            var layers = sut.Layers;

            // Assert
            layers.Should().HaveCount(1);
        }

        [Fact]
        public void Constructor_ShouldSetCurrentLayer()
        {
            // Act
            var currentLayer = sut.CurrentLayer;

            // Assert
            currentLayer.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldSetCurrentLayerName()
        {
            // Act
            var layerName = sut.CurrentLayer?.Name;

            // Assert
            layerName.Should().Be("Layer 1");
        }

        [Fact]
        public void AddLayer_ShouldIncreaseLayerCount()
        {
            // Arrange
            // (SUT initialized in constructor)

            // Act
            sut.AddLayer();

            // Assert
            sut.Layers.Should().HaveCount(2);
        }

        [Fact]
        public void AddLayer_ShouldChangeCurrentLayer()
        {
            // Arrange
            var initialLayer = sut.CurrentLayer;

            // Act
            sut.AddLayer();

            // Assert
            sut.CurrentLayer.Should().NotBe(initialLayer);
        }

        [Fact]
        public void AddLayer_ShouldSetNewLayerAsCurrent()
        {
            // Act
            sut.AddLayer();

            // Assert
            sut.CurrentLayer!.Name.Should().Be("Layer 2");
        }

        [Fact]
        public void RemoveLayer_ShouldDecreaseLayerCount()
        {
            // Arrange
            sut.AddLayer();
            var layerToRemove = sut.CurrentLayer!;

            // Act
            sut.RemoveLayer(layerToRemove);

            // Assert
            sut.Layers.Should().HaveCount(1);
        }

        [Fact]
        public void RemoveLayer_ShouldSetFirstLayerAsCurrent()
        {
            // Arrange
            sut.AddLayer(); // Layers: L1, L2(current)
            var layerToRemove = sut.CurrentLayer!;

            // Act
            sut.RemoveLayer(layerToRemove);

            // Assert
            sut.CurrentLayer!.Name.Should().Be("Layer 1");
        }

        [Fact]
        public void RemoveLayer_WhenOnlyOneLayer_ShouldNotRemove()
        {
            // Arrange
            var layer1 = sut.CurrentLayer!;

            // Act
            sut.RemoveLayer(layer1);

            // Assert
            sut.Layers.Should().HaveCount(1);
        }

        [Fact]
        public void DrawingStateChangedMessage_ShouldTriggerHistorySave()
        {
            // Arrange
            // HistoryManager saves state in constructor of LayerStateManager.
            // So, CanUndo should be false initially because historyIndex is 0.
            sut.HistoryManager.CanUndo.Should().BeFalse(); // FIX HERE
            sut.HistoryManager.CanRedo.Should().BeFalse(); // No redo possible yet

            // Act
            drawingStateSubject.OnNext(new DrawingStateChangedMessage());

            // Assert
            // After another state save, CanUndo should now be true.
            // If there were any redo states, they should be cleared, so CanRedo should be false
            sut.HistoryManager.CanUndo.Should().BeTrue(); // FIX HERE
            sut.HistoryManager.CanRedo.Should().BeFalse();
        }
    }
}