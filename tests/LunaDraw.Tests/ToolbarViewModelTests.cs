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

using CommunityToolkit.Maui.Storage;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.ViewModels;
using Moq;
using ReactiveUI;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit;

namespace LunaDraw.Tests
{
  public class ToolbarViewModelTests
  {
    private readonly Mock<ILayerFacade> layerFacadeMock;
    private readonly Mock<IMessageBus> messageBusMock;
    private readonly Mock<IBitmapCache> bitmapCacheMock;
    private readonly Mock<IFileSaver> fileSaverMock;
    private readonly NavigationModel navigationModel;
    private readonly SelectionViewModel selectionViewModel;
    private readonly HistoryViewModel historyViewModel;
    private readonly HistoryMemento historyMemento;

    public ToolbarViewModelTests()
    {
      layerFacadeMock = new Mock<ILayerFacade>();
      messageBusMock = new Mock<IMessageBus>();
      bitmapCacheMock = new Mock<IBitmapCache>();
      fileSaverMock = new Mock<IFileSaver>();
      navigationModel = new NavigationModel();
      historyMemento = new HistoryMemento();

      layerFacadeMock.Setup(x => x.Layers).Returns(new ObservableCollection<Layer>());
      layerFacadeMock.Setup(x => x.HistoryMemento).Returns(historyMemento);

      // Setup MessageBus to avoid NullReference in ToolbarViewModel constructor
      messageBusMock.Setup(x => x.Listen<BrushSettingsChangedMessage>())
          .Returns(Observable.Empty<BrushSettingsChangedMessage>());
      messageBusMock.Setup(x => x.Listen<BrushShapeChangedMessage>())
          .Returns(Observable.Empty<BrushShapeChangedMessage>());

      // Setup dependencies for ViewModels
      var selectionObserver = new SelectionObserver();
      var clipboardManager = new ClipboardMemento();
      selectionViewModel = new SelectionViewModel(selectionObserver, layerFacadeMock.Object, clipboardManager, messageBusMock.Object);

      historyViewModel = new HistoryViewModel(layerFacadeMock.Object, messageBusMock.Object);
    }

    [Fact]
    public void SaveImageCommand_ShouldNotExecute_WhenCanvasSizeIsZero()
    {
      // Arrange
      navigationModel.CanvasWidth = 0;
      navigationModel.CanvasHeight = 0;

      var viewModel = new ToolbarViewModel(
          layerFacadeMock.Object,
          selectionViewModel,
          historyViewModel,
          messageBusMock.Object,
          bitmapCacheMock.Object,
          navigationModel,
          fileSaverMock.Object);

      // Act
      viewModel.SaveImageCommand.Execute().Subscribe();

      // Assert
      fileSaverMock.Verify(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveImageCommand_ShouldExecute_WhenCanvasSizeIsValid()
    {
      // Arrange
      navigationModel.CanvasWidth = 100;
      navigationModel.CanvasHeight = 100;

      var viewModel = new ToolbarViewModel(
          layerFacadeMock.Object,
          selectionViewModel,
          historyViewModel,
          messageBusMock.Object,
          bitmapCacheMock.Object,
          navigationModel,
          fileSaverMock.Object);

      fileSaverMock.Setup(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new FileSaverResult("path", null));

      // Act
      await viewModel.SaveImageCommand.Execute().ToTask();

      // Assert
      fileSaverMock.Verify(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }
  }
}
