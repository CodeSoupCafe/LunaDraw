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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using LunaDraw.Logic.Utils;
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
        private readonly LayerFacade layerFacade;
        private readonly LayerPanelViewModel viewModel;
        private readonly Subject<DrawingStateChangedMessage> drawingStateSubject;

        public LayerPanelViewModelTests()
        {
            mockBus = new Mock<IMessageBus>();
            drawingStateSubject = new Subject<DrawingStateChangedMessage>();
            mockBus.Setup(x => x.Listen<DrawingStateChangedMessage>()).Returns(drawingStateSubject);

            layerFacade = new LayerFacade(mockBus.Object);
            viewModel = new LayerPanelViewModel(layerFacade, mockBus.Object);
        }

        [Fact]
        public void AddLayer_ShouldUpdateCurrentLayerInViewModel()
        {
            // Act
            viewModel.AddLayerCommand.Execute().Subscribe();

            // Assert
            Assert.Equal(2, layerFacade.Layers.Count);
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
            Assert.Single(layerFacade.Layers);
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

        [Fact]
        public void IsTransparentBackground_ShouldToggleAndInvalidateCanvas()
        {
            // Arrange
            var invalidationCount = 0;
            mockBus.Setup(x => x.SendMessage(It.IsAny<CanvasInvalidateMessage>(), It.IsAny<string>()))
                   .Callback<CanvasInvalidateMessage, string>((_, __) => invalidationCount++);
            
            // Initial state check (defaults to true)
            Assert.True(viewModel.IsTransparentBackground);

            // Act
            viewModel.IsTransparentBackground = false;

            // Assert
            Assert.False(viewModel.IsTransparentBackground);
            Assert.Equal(1, invalidationCount);

            // Act 2
            viewModel.IsTransparentBackground = true;

            // Assert
            Assert.True(viewModel.IsTransparentBackground);
            Assert.Equal(2, invalidationCount);
        }
    }
}
