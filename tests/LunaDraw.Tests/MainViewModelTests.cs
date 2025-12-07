using System.Collections.ObjectModel;
using System.Reactive.Linq;

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Services;
using LunaDraw.Logic.ViewModels;
using Moq;
using ReactiveUI;
using SkiaSharp;
using Xunit;

namespace LunaDraw.Tests
{
    public class MainViewModelTests
    {
        private readonly Mock<IToolStateManager> toolStateManagerMock;
        private readonly Mock<ILayerStateManager> layerStateManagerMock;
        private readonly Mock<ICanvasInputHandler> canvasInputHandlerMock;
        private readonly NavigationModel navigationModel;
        private readonly SelectionManager selectionManager;
        private readonly Mock<IMessageBus> messageBusMock;
        private readonly MainViewModel viewModel;
        private readonly ObservableCollection<Layer> layers;
        private readonly HistoryManager historyManager;
        
        // Sub-VMs
        private readonly LayerPanelViewModel layerPanelVM;
        private readonly SelectionViewModel selectionVM;
        private readonly HistoryViewModel historyVM;

        public MainViewModelTests()
        {
            toolStateManagerMock = new Mock<IToolStateManager>();
            layerStateManagerMock = new Mock<ILayerStateManager>();
            canvasInputHandlerMock = new Mock<ICanvasInputHandler>();
            navigationModel = new NavigationModel();
            selectionManager = new SelectionManager();
            messageBusMock = new Mock<IMessageBus>();
            layers = new ObservableCollection<Layer>();
            historyManager = new HistoryManager(); // Real HistoryManager for integration test

            layerStateManagerMock.Setup(m => m.Layers).Returns(layers);
            layerStateManagerMock.Setup(m => m.HistoryManager).Returns(historyManager);
            // Setup property change notifications for ToolStateManager
            toolStateManagerMock.As<System.ComponentModel.INotifyPropertyChanged>();

            // Create Sub-VMs (we can test them in isolation, but here we test MainVM integration)
            // Using real instances for VM logic
            layerPanelVM = new LayerPanelViewModel(layerStateManagerMock.Object, messageBusMock.Object);
            selectionVM = new SelectionViewModel(selectionManager, layerStateManagerMock.Object, new ClipboardManager(), messageBusMock.Object);
            historyVM = new HistoryViewModel(layerStateManagerMock.Object, messageBusMock.Object);

            viewModel = new MainViewModel(
                toolStateManagerMock.Object,
                layerStateManagerMock.Object,
                canvasInputHandlerMock.Object,
                navigationModel,
                selectionManager,
                messageBusMock.Object,
                layerPanelVM,
                selectionVM,
                historyVM
            );
        }

        [Fact]
        public void DeleteSelectedCommand_ShouldRemoveElements_WhenExecuted_ViaSelectionVM()
        {
            // Arrange
            var layer = new Layer();
            var element = new DrawableRectangle();
            layer.Elements.Add(element);
            layers.Add(layer);

            layerStateManagerMock.SetupGet(x => x.CurrentLayer).Returns(layer);
            
            selectionManager.Add(element);

            // Act
            // Executing command on SelectionVM which is exposed
            viewModel.SelectionVM.DeleteSelectedCommand.Execute().Subscribe();

            // Assert
            Assert.DoesNotContain(element, layer.Elements);
            Assert.Empty(selectionManager.Selected);
            messageBusMock.Verify(x => x.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Once);
            layerStateManagerMock.Verify(x => x.SaveState(), Times.Once);
        }

        [Fact]
        public void GroupSelectedCommand_ShouldGroupElements_WhenExecuted_ViaSelectionVM()
        {
            // Arrange
            var layer = new Layer();
            var element1 = new DrawableRectangle();
            var element2 = new DrawableRectangle();
            layer.Elements.Add(element1);
            layer.Elements.Add(element2);
            layerStateManagerMock.SetupGet(x => x.CurrentLayer).Returns(layer);
            
            selectionManager.Add(element1);
            selectionManager.Add(element2);

            // Act
            viewModel.SelectionVM.GroupSelectedCommand.Execute().Subscribe();

            // Assert
            Assert.DoesNotContain(element1, layer.Elements);
            Assert.DoesNotContain(element2, layer.Elements);
            Assert.Single(layer.Elements, e => e is DrawableGroup);
            Assert.Single(selectionManager.Selected);
            Assert.IsType<DrawableGroup>(selectionManager.Selected.First());
        }
        
        [Fact]
        public void PasteCommand_ShouldAddClonedElement_WhenClipboardHasItems_ViaSelectionVM()
        {
            // Arrange
            var layer = new Layer();
            var element = new DrawableRectangle();
            layer.Elements.Add(element);
            layers.Add(layer);
            layerStateManagerMock.SetupGet(x => x.CurrentLayer).Returns(layer);

            selectionManager.Add(element);

            // Copy first
            viewModel.SelectionVM.CopyCommand.Execute().Subscribe();
            
            // Clear selection to verify paste adds new one
            selectionManager.Clear();

            // Act
            viewModel.SelectionVM.PasteCommand.Execute().Subscribe();

            // Assert
            Assert.Equal(2, layer.Elements.Count);
            messageBusMock.Verify(x => x.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.AtLeastOnce);
        }
    }
}