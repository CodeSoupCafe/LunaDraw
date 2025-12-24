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
using SkiaSharp;
using Xunit;

namespace LunaDraw.Tests
{
  public class SelectionViewModelTests
  {
    private readonly Mock<IMessageBus> mockBus;
    private readonly LayerFacade layerFacade;
    private readonly SelectionObserver selectionObserver;
    private readonly ClipboardMemento clipboardMemento;
    private readonly SelectionViewModel viewModel;

    public SelectionViewModelTests()
    {
      mockBus = new Mock<IMessageBus>();
      // Setup generic listeners to avoid null reference exceptions
      mockBus.Setup(x => x.Listen<DrawingStateChangedMessage>()).Returns(Observable.Empty<DrawingStateChangedMessage>());
      mockBus.Setup(x => x.Listen<SelectionChangedMessage>()).Returns(Observable.Empty<SelectionChangedMessage>());

      var mockRecordingHandler = new Moq.Mock<LunaDraw.Logic.Handlers.IRecordingHandler>();
      layerFacade = new LayerFacade(mockBus.Object, mockRecordingHandler.Object);
      selectionObserver = new SelectionObserver();
      clipboardMemento = new ClipboardMemento();

      viewModel = new SelectionViewModel(selectionObserver, layerFacade, clipboardMemento, mockBus.Object);
    }

    [Fact]
    public void MoveSelectionToNewLayer_ShouldCreateLayerAndMoveElements()
    {
      // Arrange
      var layer1 = layerFacade.Layers.First();
      var element = new DrawableRectangle { Rectangle = new SKRect(0, 0, 10, 10) };
      layer1.Elements.Add(element);

      selectionObserver.Add(element);

      Assert.Single(layerFacade.Layers);
      Assert.Contains(element, layer1.Elements);

      // Act
      viewModel.MoveSelectionToNewLayerCommand.Execute().Subscribe();

      // Assert
      Assert.Equal(2, layerFacade.Layers.Count);
      var layer2 = layerFacade.Layers[1];

      // Element should be in Layer 2
      Assert.Contains(element, layer2.Elements);
      // Element should NOT be in Layer 1
      Assert.DoesNotContain(element, layer1.Elements);
    }

    [Fact]
    public void DuplicateCommand_ShouldCopyAndPasteElement()
    {
      // Arrange
      var layer = layerFacade.Layers.First();
      var element = new DrawableRectangle { Rectangle = new SKRect(0, 0, 10, 10) };
      layer.Elements.Add(element);

      selectionObserver.Add(element);

      // Act
      viewModel.DuplicateCommand.Execute().Subscribe();

      // Assert
      Assert.Equal(2, layer.Elements.Count);
      var clone = layer.Elements[1];

      Assert.NotSame(element, clone);
      Assert.IsType<DrawableRectangle>(clone);
      var cloneBounds = clone.Bounds;

      // Paste adds (10,10) offset
      Assert.Equal(element.Bounds.Left + 10, cloneBounds.Left);
      Assert.Equal(element.Bounds.Top + 10, cloneBounds.Top);
    }
  }
}