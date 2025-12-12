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

using System;
using System.Linq;
using System.Reactive.Subjects;

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Services; // Keep this for LayerFacade
using Moq;
using Xunit;
using ReactiveUI; // ADDED: Required for IMessageBus

namespace LunaDraw.Tests
{
    public class LayerFacadeTests
    {
        private readonly Mock<IMessageBus> mockBus;
        private readonly Subject<DrawingStateChangedMessage> drawingStateSubject;
        private readonly LayerFacade layerFacade;

        public LayerFacadeTests()
        {
            mockBus = new Mock<IMessageBus>();
            drawingStateSubject = new Subject<DrawingStateChangedMessage>();

            mockBus.Setup(x => x.Listen<DrawingStateChangedMessage>())
                .Returns(drawingStateSubject);

            layerFacade = new LayerFacade(mockBus.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeWithOneLayer()
        {
            // Act
            var layers = layerFacade.Layers;

            // Assert
            Assert.Single(layers);
        }

        [Fact]
        public void Constructor_ShouldSetCurrentLayer()
        {
            // Act
            var currentLayer = layerFacade.CurrentLayer;

            // Assert
            Assert.NotNull(currentLayer);
        }

        [Fact]
        public void Constructor_ShouldSetCurrentLayerName()
        {
            // Act
            var layerName = layerFacade.CurrentLayer?.Name;

            // Assert
            Assert.Equal("Layer 1", layerName);
        }

        [Fact]
        public void AddLayer_ShouldIncreaseLayerCount()
        {
            // Arrange
            // (LayerFacade initialized in constructor)

            // Act
            layerFacade.AddLayer();

            // Assert
            Assert.Equal(2, layerFacade.Layers.Count);
        }

        [Fact]
        public void AddLayer_ShouldChangeCurrentLayer()
        {
            // Arrange
            var initialLayer = layerFacade.CurrentLayer;

            // Act
            layerFacade.AddLayer();

            // Assert
            Assert.NotEqual(initialLayer, layerFacade.CurrentLayer);
        }

        [Fact]
        public void AddLayer_ShouldSetNewLayerAsCurrent()
        {
            // Act
            layerFacade.AddLayer();

            // Assert
            Assert.Equal("Layer 2", layerFacade.CurrentLayer!.Name);
        }

        [Fact]
        public void RemoveLayer_ShouldDecreaseLayerCount()
        {
            // Arrange
            layerFacade.AddLayer();
            var layerToRemove = layerFacade.CurrentLayer!;

            // Act
            layerFacade.RemoveLayer(layerToRemove);

            // Assert
            Assert.Single(layerFacade.Layers);
        }

        [Fact]
        public void RemoveLayer_ShouldSetFirstLayerAsCurrent()
        {
            // Arrange
            layerFacade.AddLayer(); // Layers: L1, L2(current)
            var layerToRemove = layerFacade.CurrentLayer!;

            // Act
            layerFacade.RemoveLayer(layerToRemove);

            // Assert
            Assert.Equal("Layer 1", layerFacade.CurrentLayer!.Name);
        }

        [Fact]
        public void RemoveLayer_WhenOnlyOneLayer_ShouldNotRemove()
        {
            // Arrange
            var layer1 = layerFacade.CurrentLayer!;

            // Act
            layerFacade.RemoveLayer(layer1);

            // Assert
            Assert.Single(layerFacade.Layers);
        }

        [Fact]
        public void DrawingStateChangedMessage_ShouldTriggerHistorySave()
        {
            // Arrange
            // HistoryMemento saves state in constructor of LayerFacade.
            // So, CanUndo should be false initially because historyIndex is 0.
            Assert.False(layerFacade.HistoryMemento.CanUndo); // FIX HERE
            Assert.False(layerFacade.HistoryMemento.CanRedo); // No redo possible yet

            // Act
            drawingStateSubject.OnNext(new DrawingStateChangedMessage());

            // Assert
            // After another state save, CanUndo should now be true.
            // If there were any redo states, they should be cleared, so CanRedo should be false
            Assert.True(layerFacade.HistoryMemento.CanUndo); // FIX HERE
            Assert.False(layerFacade.HistoryMemento.CanRedo);
        }
    }
}