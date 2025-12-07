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
  public class MainViewModel : ReactiveObject
  {
    // Dependencies
    public IToolStateManager ToolStateManager { get; }
    public ILayerStateManager LayerStateManager { get; }
    public ICanvasInputHandler CanvasInputHandler { get; }
    public NavigationModel NavigationModel { get; }
    public SelectionManager SelectionManager { get; }
    private readonly IMessageBus messageBus;

    // Sub-ViewModels
    public LayerPanelViewModel LayerPanelVM { get; }
    public SelectionViewModel SelectionVM { get; }
    public HistoryViewModel HistoryVM { get; }

    public SKRect CanvasSize { get; set; }

    public MainViewModel(
        IToolStateManager toolStateManager,
        ILayerStateManager layerStateManager,
        ICanvasInputHandler canvasInputHandler,
        NavigationModel navigationModel,
        SelectionManager selectionManager,
        IMessageBus messageBus,
        LayerPanelViewModel layerPanelVM,
        SelectionViewModel selectionVM,
        HistoryViewModel historyVM)
    {
      ToolStateManager = toolStateManager;
      LayerStateManager = layerStateManager;
      CanvasInputHandler = canvasInputHandler;
      NavigationModel = navigationModel;
      SelectionManager = selectionManager;
      this.messageBus = messageBus;

      LayerPanelVM = layerPanelVM;
      SelectionVM = selectionVM;
      HistoryVM = historyVM;
    }

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
            // Ensure the dragged layer stays selected
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
        Scale = NavigationModel.TotalMatrix.ScaleX,
        IsGlowEnabled = ToolStateManager.IsGlowEnabled,
        GlowColor = ToolStateManager.GlowColor,
        GlowRadius = ToolStateManager.GlowRadius,
        IsRainbowEnabled = ToolStateManager.IsRainbowEnabled,
        ScatterRadius = ToolStateManager.ScatterRadius,
        SizeJitter = ToolStateManager.SizeJitter,
        AngleJitter = ToolStateManager.AngleJitter,
        HueJitter = ToolStateManager.HueJitter,
        CanvasMatrix = NavigationModel.UserMatrix
      };
    }
  }
}
