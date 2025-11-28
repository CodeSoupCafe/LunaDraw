using System.Collections.ObjectModel;

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Services;
using LunaDraw.Logic.Tools;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Tests
{
  public class CanvasInputHandlerTests
  {
    [Fact]
    public void HandleMultiTouch_MissingTouchKey_ThrowsKeyNotFoundException()
    {
      // Arrange
      var toolStateManager = new StubToolStateManager();
      var layerStateManager = new StubLayerStateManager();
      layerStateManager.CurrentLayer = new Layer(); // Ensure we have a layer

      var selectionManager = new SelectionManager();
      var navigationModel = new NavigationModel();
      var messageBus = new StubMessageBus();

      var handler = new CanvasInputHandler(
          toolStateManager,
          layerStateManager,
          selectionManager,
          navigationModel,
          messageBus
      );

      // Simulate two touches being pressed (ID 1 and 2)
      var touch1 = new SKTouchEventArgs(1, SKTouchAction.Pressed, new SKPoint(10, 10), true);
      handler.ProcessTouch(touch1, SKRect.Empty, null);

      var touch2 = new SKTouchEventArgs(2, SKTouchAction.Pressed, new SKPoint(20, 20), true);
      handler.ProcessTouch(touch2, SKRect.Empty, null);

      // Simulate a moved event for a third touch (ID 3) that was never pressed
      // This causes _activeTouches.Count (2) >= 2 to be true, triggering HandleMultiTouch(touch3)
      var touch3 = new SKTouchEventArgs(3, SKTouchAction.Moved, new SKPoint(30, 30), true);

      // Act & Assert
      Assert.Throws<KeyNotFoundException>(() => handler.ProcessTouch(touch3, SKRect.Empty, null));
    }

    // Stubs
    private class StubToolStateManager : IToolStateManager
    {
      public IDrawingTool ActiveTool { get; set; } = new StubDrawingTool();
      public SKColor StrokeColor { get; set; }
      public SKColor? FillColor { get; set; }
      public float StrokeWidth { get; set; }
      public byte Opacity { get; set; }
      public byte Flow { get; set; }
      public float Spacing { get; set; }
      public BrushShape CurrentBrushShape { get; set; } = default!;
      public List<IDrawingTool> AvailableTools { get; } = new List<IDrawingTool>();
      public List<BrushShape> AvailableBrushShapes { get; } = new List<BrushShape>();
    }

    private class StubLayerStateManager : ILayerStateManager
    {
      public ObservableCollection<Layer> Layers { get; } = new ObservableCollection<Layer>();
      public Layer? CurrentLayer { get; set; }
      public HistoryManager HistoryManager { get; } = default!;
      public void AddLayer() { }
      public void RemoveLayer(Layer layer) { }
      public void SaveState() { }
    }

    private class StubMessageBus : IMessageBus
    {
      public IObservable<T> Listen<T>(string? contract = null) => System.Reactive.Linq.Observable.Empty<T>();
      public bool IsRegistered(Type type, string? contract = null) => false;
      public IDisposable RegisterMessageSource<T>(IObservable<T> source, string? contract = null) => System.Reactive.Disposables.Disposable.Empty;
      public void SendMessage<T>(T message, string? contract = null) { }
      public void RegisterScheduler<T>(System.Reactive.Concurrency.IScheduler scheduler, string? contract = null) { }
      public IObservable<T> ListenIncludeLatest<T>(string? contract = null) => System.Reactive.Linq.Observable.Empty<T>();
    }

    private class StubDrawingTool : IDrawingTool
    {
      public string Name => "Stub";
      public string Icon => "";
      public ToolType Type => ToolType.Freehand;
      public void OnTouchPressed(SKPoint point, ToolContext context) { }
      public void OnTouchMoved(SKPoint point, ToolContext context) { }
      public void OnTouchReleased(SKPoint point, ToolContext context) { }
      public void OnTouchCancelled(ToolContext context) { }
      public void DrawPreview(SKCanvas canvas, LunaDraw.Logic.ViewModels.MainViewModel viewModel) { }
    }
  }
}
