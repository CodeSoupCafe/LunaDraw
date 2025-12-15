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

using System.Collections.ObjectModel;
using System.Reactive.Linq;

using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;
using Moq;
using ReactiveUI;
using System.Reactive.Concurrency;
using CommunityToolkit.Maui.Storage;
using SkiaSharp;
using Xunit;

namespace LunaDraw.Tests
{
  public class MainViewModelTests
  {
    private readonly Mock<ToolbarViewModel> mockToolbarViewModel;
    private readonly Mock<ILayerFacade> layerFacadeMock;
    private readonly Mock<ICanvasInputHandler> canvasInputHandlerMock;
    private readonly NavigationModel navigationModel;
    private readonly SelectionObserver selectionObserver;
    private readonly Mock<IMessageBus> messageBusMock;
    private readonly MainViewModel viewModel;
    private readonly ObservableCollection<Layer> layers;
    private readonly HistoryMemento historyMemento;

    // Sub-VMs
    private readonly LayerPanelViewModel layerPanelVM;
    private readonly SelectionViewModel selectionVM;
    private readonly HistoryViewModel historyVM;

    public MainViewModelTests()
    {
      RxApp.MainThreadScheduler = Scheduler.Immediate;

      layerFacadeMock = new Mock<ILayerFacade>();
      canvasInputHandlerMock = new Mock<ICanvasInputHandler>();
      navigationModel = new NavigationModel();
      selectionObserver = new SelectionObserver();
      messageBusMock = new Mock<IMessageBus>();
      layers = new ObservableCollection<Layer>();
      historyMemento = new HistoryMemento(); // Real HistoryMemento for integration test

      layerFacadeMock.Setup(m => m.Layers).Returns(layers);
      layerFacadeMock.Setup(m => m.HistoryMemento).Returns(historyMemento);

      var preferencesFacadeMock = new Mock<IPreferencesFacade>();

      // Create Sub-VMs (we can test them in isolation, but here we test MainVM integration)
      // Using real instances for VM logic
      layerPanelVM = new LayerPanelViewModel(layerFacadeMock.Object, messageBusMock.Object, preferencesFacadeMock.Object);
      selectionVM = new SelectionViewModel(selectionObserver, layerFacadeMock.Object, new ClipboardMemento(), messageBusMock.Object);
      historyVM = new HistoryViewModel(layerFacadeMock.Object, messageBusMock.Object);

      // Initialize ToolbarViewModel Mock with dependencies
      // We need to verify that messageBus.Listen returns observables because ToolbarViewModel constructor subscribes to them
      messageBusMock.Setup(x => x.Listen<BrushSettingsChangedMessage>()).Returns(Observable.Empty<BrushSettingsChangedMessage>());
      messageBusMock.Setup(x => x.Listen<BrushShapeChangedMessage>()).Returns(Observable.Empty<BrushShapeChangedMessage>());
      messageBusMock.Setup(x => x.Listen<ViewOptionsChangedMessage>()).Returns(Observable.Empty<ViewOptionsChangedMessage>());
      messageBusMock.Setup(x => x.Listen<ShowAdvancedSettingsMessage>()).Returns(Observable.Empty<ShowAdvancedSettingsMessage>());

      mockToolbarViewModel = new Mock<ToolbarViewModel>(
          layerFacadeMock.Object,
          selectionVM,
          historyVM,
          messageBusMock.Object,
          new Mock<IBitmapCache>().Object,
          navigationModel,
          new Mock<IFileSaver>().Object,
          preferencesFacadeMock.Object
      );

      // Setup property change notifications for ToolStateManager
      mockToolbarViewModel.As<System.ComponentModel.INotifyPropertyChanged>();

      viewModel = new MainViewModel(
          mockToolbarViewModel.Object,
          layerFacadeMock.Object,
          canvasInputHandlerMock.Object,
          navigationModel,
          selectionObserver,
          messageBusMock.Object,
          preferencesFacadeMock.Object,
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

      layerFacadeMock.SetupGet(x => x.CurrentLayer).Returns(layer);

      selectionObserver.Add(element);

      messageBusMock.Invocations.Clear();

      // Act
      // Executing command on SelectionVM which is exposed
      viewModel.SelectionVM.DeleteSelectedCommand.Execute().Subscribe();

      // Assert
      Assert.DoesNotContain(element, layer.Elements);
      Assert.Empty(selectionObserver.Selected);
      messageBusMock.Verify(x => x.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Once);
      layerFacadeMock.Verify(x => x.SaveState(), Times.Once);
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
      layerFacadeMock.SetupGet(x => x.CurrentLayer).Returns(layer);

      selectionObserver.Add(element1);
      selectionObserver.Add(element2);

      // Act
      viewModel.SelectionVM.GroupSelectedCommand.Execute().Subscribe();

      // Assert
      Assert.DoesNotContain(element1, layer.Elements);
      Assert.DoesNotContain(element2, layer.Elements);
      Assert.Single(layer.Elements, e => e is DrawableGroup);
      Assert.Single(selectionObserver.Selected);
      Assert.IsType<DrawableGroup>(selectionObserver.Selected.First());
    }

    [Fact]
    public void PasteCommand_ShouldAddClonedElement_WhenClipboardHasItems_ViaSelectionVM()
    {
      // Arrange
      var layer = new Layer();
      var element = new DrawableRectangle();
      layer.Elements.Add(element);
      layers.Add(layer);
      layerFacadeMock.SetupGet(x => x.CurrentLayer).Returns(layer);

      selectionObserver.Add(element);

      // Copy first
      viewModel.SelectionVM.CopyCommand.Execute().Subscribe();

      // Clear selection to verify paste adds new one
      selectionObserver.Clear();

      // Act
      viewModel.SelectionVM.PasteCommand.Execute().Subscribe();

      // Assert
      Assert.Equal(2, layer.Elements.Count);
      messageBusMock.Verify(x => x.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.AtLeastOnce);
    }
  }
}
