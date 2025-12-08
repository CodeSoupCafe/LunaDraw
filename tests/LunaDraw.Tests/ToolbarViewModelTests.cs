using CommunityToolkit.Maui.Storage;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.ViewModels;
using Moq;
using ReactiveUI;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Reactive.Threading.Tasks;
using Xunit;

namespace LunaDraw.Tests
{
    public class ToolbarViewModelTests
    {
        private readonly Mock<IToolStateManager> toolStateManagerMock;
        private readonly Mock<ILayerStateManager> layerStateManagerMock;
        private readonly Mock<IMessageBus> messageBusMock;
        private readonly Mock<IBitmapCacheManager> bitmapCacheManagerMock;
        private readonly Mock<IFileSaver> fileSaverMock;
        private readonly NavigationModel navigationModel;
        private readonly SelectionViewModel selectionViewModel;
        private readonly HistoryViewModel historyViewModel;
        private readonly HistoryManager historyManager;

        public ToolbarViewModelTests()
        {
            toolStateManagerMock = new Mock<IToolStateManager>();
            layerStateManagerMock = new Mock<ILayerStateManager>();
            messageBusMock = new Mock<IMessageBus>();
            bitmapCacheManagerMock = new Mock<IBitmapCacheManager>();
            fileSaverMock = new Mock<IFileSaver>();
            navigationModel = new NavigationModel();
            historyManager = new HistoryManager();

            // Setup default behavior for mocks
            toolStateManagerMock.Setup(x => x.AvailableTools).Returns(new List<IDrawingTool>());
            toolStateManagerMock.Setup(x => x.AvailableBrushShapes).Returns(new List<BrushShape>());
            
            layerStateManagerMock.Setup(x => x.Layers).Returns(new ObservableCollection<Layer>());
            layerStateManagerMock.Setup(x => x.HistoryManager).Returns(historyManager);
            
            // Setup dependencies for ViewModels
            var selectionManager = new SelectionManager();
            var clipboardManager = new ClipboardManager();
            selectionViewModel = new SelectionViewModel(selectionManager, layerStateManagerMock.Object, clipboardManager, messageBusMock.Object);
            
            historyViewModel = new HistoryViewModel(layerStateManagerMock.Object, messageBusMock.Object);
        }

        [Fact]
        public void SaveImageCommand_ShouldNotExecute_WhenCanvasSizeIsZero()
        {
            // Arrange
            navigationModel.CanvasWidth = 0;
            navigationModel.CanvasHeight = 0;

            var viewModel = new ToolbarViewModel(
                toolStateManagerMock.Object,
                layerStateManagerMock.Object,
                selectionViewModel,
                historyViewModel,
                messageBusMock.Object,
                bitmapCacheManagerMock.Object,
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
                toolStateManagerMock.Object,
                layerStateManagerMock.Object,
                selectionViewModel,
                historyViewModel,
                messageBusMock.Object,
                bitmapCacheManagerMock.Object,
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
