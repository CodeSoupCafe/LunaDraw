using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Services;
using LunaDraw.Logic.Tools;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Logic.ViewModels
{
  public class MainViewModel(
      IToolStateManager toolStateManager,
      ILayerStateManager layerStateManager,
      ICanvasInputHandler canvasInputHandler,
      NavigationModel navigationModel,
      SelectionManager selectionManager,
      IMessageBus messageBus,
      LayerPanelViewModel layerPanelVM,
      SelectionViewModel selectionVM,
      HistoryViewModel historyVM) : ReactiveObject
  {
    // Dependencies
    public IToolStateManager ToolStateManager { get; } = toolStateManager;
    public ILayerStateManager LayerStateManager { get; } = layerStateManager;
    public ICanvasInputHandler CanvasInputHandler { get; } = canvasInputHandler;
    public NavigationModel NavigationModel { get; } = navigationModel;
    public SelectionManager SelectionManager { get; } = selectionManager;
    private readonly IMessageBus messageBus = messageBus;

    // Sub-ViewModels
    public LayerPanelViewModel LayerPanelVM { get; } = layerPanelVM;
    public SelectionViewModel SelectionVM { get; } = selectionVM;
    public HistoryViewModel HistoryVM { get; } = historyVM;

    public SKRect CanvasSize { get; set; }

    // Facades for View/CodeBehind access
    public ObservableCollection<Layer> Layers => LayerStateManager.Layers;

    public Layer? CurrentLayer
    {
      get => LayerStateManager.CurrentLayer;
      set => LayerStateManager.CurrentLayer = value;
    }

    public IDrawingTool ActiveTool
    {
      get => ToolStateManager.ActiveTool;
      set => ToolStateManager.ActiveTool = value;
    }

    public ReadOnlyObservableCollection<IDrawableElement> SelectedElements => SelectionManager.Selected;

    public void ReorderLayer(Layer source, Layer target)
    {
      if (source == null || target == null || source == target) return;
      int oldIndex = Layers.IndexOf(source);
      int newIndex = Layers.IndexOf(target);
      if (oldIndex >= 0 && newIndex >= 0)
      {
        LayerStateManager.MoveLayer(oldIndex, newIndex);
        CurrentLayer = source;
      }
    }

    public void ProcessTouch(SKTouchEventArgs e)
    {
      CanvasInputHandler.ProcessTouch(e, CanvasSize);
    }

    public ToolContext CreateToolContext()
    {
      return new ToolContext
      {
        CurrentLayer = LayerStateManager.CurrentLayer!,
        StrokeColor = ToolStateManager.StrokeColor,
        FillColor = ToolStateManager.FillColor,
        StrokeWidth = ToolStateManager.StrokeWidth,
        Opacity = ToolStateManager.Opacity,
        Flow = ToolStateManager.Flow,
        Spacing = ToolStateManager.Spacing,
        BrushShape = ToolStateManager.CurrentBrushShape,
        AllElements = LayerStateManager.Layers.SelectMany(l => l.Elements),
        Layers = LayerStateManager.Layers,
        SelectionManager = SelectionManager,
        Scale = NavigationModel.ViewMatrix.ScaleX,
        IsGlowEnabled = ToolStateManager.IsGlowEnabled,
        GlowColor = ToolStateManager.GlowColor,
        GlowRadius = ToolStateManager.GlowRadius,
        IsRainbowEnabled = ToolStateManager.IsRainbowEnabled,
        ScatterRadius = ToolStateManager.ScatterRadius,
        SizeJitter = ToolStateManager.SizeJitter,
        AngleJitter = ToolStateManager.AngleJitter,
        HueJitter = ToolStateManager.HueJitter,
        CanvasMatrix = NavigationModel.ViewMatrix
      };
    }
  }
}