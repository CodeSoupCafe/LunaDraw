using System.Collections.ObjectModel;
using System.Reactive.Linq;
using FluentAssertions;
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
        private readonly Mock<IToolStateManager> _toolStateManagerMock;
        private readonly Mock<ILayerStateManager> _layerStateManagerMock;
        private readonly Mock<ICanvasInputHandler> _canvasInputHandlerMock;
        private readonly NavigationModel _navigationModel;
        private readonly SelectionManager _selectionManager;
        private readonly Mock<IMessageBus> _messageBusMock;
        private readonly MainViewModel _viewModel;
        private readonly ObservableCollection<Layer> _layers;
        private readonly HistoryManager _historyManager;
        
        // Sub-VMs
        private readonly LayerPanelViewModel _layerPanelVM;
        private readonly SelectionViewModel _selectionVM;
        private readonly HistoryViewModel _historyVM;

        public MainViewModelTests()
        {
            _toolStateManagerMock = new Mock<IToolStateManager>();
            _layerStateManagerMock = new Mock<ILayerStateManager>();
            _canvasInputHandlerMock = new Mock<ICanvasInputHandler>();
            _navigationModel = new NavigationModel();
            _selectionManager = new SelectionManager();
            _messageBusMock = new Mock<IMessageBus>();
            _layers = new ObservableCollection<Layer>();
            _historyManager = new HistoryManager(); // Real HistoryManager for integration test

            _layerStateManagerMock.Setup(m => m.Layers).Returns(_layers);
            _layerStateManagerMock.Setup(m => m.HistoryManager).Returns(_historyManager);
            // Setup property change notifications for ToolStateManager
            _toolStateManagerMock.As<System.ComponentModel.INotifyPropertyChanged>();

            // Create Sub-VMs (we can test them in isolation, but here we test MainVM integration)
            // Using real instances for VM logic
            _layerPanelVM = new LayerPanelViewModel(_layerStateManagerMock.Object, _messageBusMock.Object);
            _selectionVM = new SelectionViewModel(_selectionManager, _layerStateManagerMock.Object, new ClipboardManager(), _messageBusMock.Object);
            _historyVM = new HistoryViewModel(_layerStateManagerMock.Object, _messageBusMock.Object);

            _viewModel = new MainViewModel(
                _toolStateManagerMock.Object,
                _layerStateManagerMock.Object,
                _canvasInputHandlerMock.Object,
                _navigationModel,
                _selectionManager,
                _messageBusMock.Object,
                _layerPanelVM,
                _selectionVM,
                _historyVM
            );
        }

        [Fact]
        public void DeleteSelectedCommand_ShouldRemoveElements_WhenExecuted_ViaSelectionVM()
        {
            // Arrange
            var layer = new Layer();
            var element = new DrawableRectangle();
            layer.Elements.Add(element);
            _layers.Add(layer);

            _layerStateManagerMock.SetupGet(x => x.CurrentLayer).Returns(layer);
            
            _selectionManager.Add(element);

            // Act
            // Executing command on SelectionVM which is exposed
            _viewModel.SelectionVM.DeleteSelectedCommand.Execute().Subscribe();

            // Assert
            layer.Elements.Should().NotContain(element);
            _selectionManager.Selected.Should().BeEmpty();
            _messageBusMock.Verify(x => x.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Once);
            _layerStateManagerMock.Verify(x => x.SaveState(), Times.Once);
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
            _layers.Add(layer);

            _layerStateManagerMock.SetupGet(x => x.CurrentLayer).Returns(layer);
            
            _selectionManager.Add(element1);
            _selectionManager.Add(element2);

            // Act
            _viewModel.SelectionVM.GroupSelectedCommand.Execute().Subscribe();

            // Assert
            layer.Elements.Should().NotContain(element1);
            layer.Elements.Should().NotContain(element2);
            layer.Elements.Should().ContainSingle(e => e is DrawableGroup);
            _selectionManager.Selected.Should().HaveCount(1);
            _selectionManager.Selected.First().Should().BeOfType<DrawableGroup>();
        }
        
        [Fact]
        public void PasteCommand_ShouldAddClonedElement_WhenClipboardHasItems_ViaSelectionVM()
        {
            // Arrange
            var layer = new Layer();
            var element = new DrawableRectangle();
            layer.Elements.Add(element);
            _layers.Add(layer);
            _layerStateManagerMock.SetupGet(x => x.CurrentLayer).Returns(layer);

            _selectionManager.Add(element);

            // Copy first
            _viewModel.SelectionVM.CopyCommand.Execute().Subscribe();
            
            // Clear selection to verify paste adds new one
            _selectionManager.Clear();

            // Act
            _viewModel.SelectionVM.PasteCommand.Execute().Subscribe();

            // Assert
            layer.Elements.Should().HaveCount(2); // Original + Paste
            _messageBusMock.Verify(x => x.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.AtLeastOnce);
        }
    }
}