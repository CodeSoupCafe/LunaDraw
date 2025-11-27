using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Utils;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Logic.ViewModels
{
  public class MainViewModel : ReactiveObject
  {
    // Navigation
    public NavigationModel NavigationModel { get; } = new NavigationModel();
    private readonly TouchManipulationManager _touchManipulationManager = new TouchManipulationManager
    {
      Mode = TouchManipulationMode.ScaleRotate
    };
    private readonly Dictionary<long, SKPoint> _activeTouches = new Dictionary<long, SKPoint>();

    // Current State
    public ObservableCollection<Layer> Layers { get; } = new ObservableCollection<Layer>();
    public List<IDrawingTool> AvailableTools { get; }
    private Layer? _currentLayer;
    public Layer? CurrentLayer
    {
      get => _currentLayer;
      set => this.RaiseAndSetIfChanged(ref _currentLayer, value);
    }

    private IDrawingTool _activeTool = new FreehandTool();
    public IDrawingTool ActiveTool
    {
      get => _activeTool;
      set => this.RaiseAndSetIfChanged(ref _activeTool, value);
    }

    private SKColor _strokeColor = SKColors.Black;
    public SKColor StrokeColor
    {
      get => _strokeColor;
      set => this.RaiseAndSetIfChanged(ref _strokeColor, value);
    }


    private SKColor? _fillColor;
    public SKColor? FillColor
    {
      get => _fillColor;
      set => this.RaiseAndSetIfChanged(ref _fillColor, value);
    }

    private float _strokeWidth = 5;
    public float StrokeWidth
    {
      get => _strokeWidth;
      set => this.RaiseAndSetIfChanged(ref _strokeWidth, value);
    }

    private byte _opacity = 255;
    public byte Opacity
    {
      get => _opacity;
      set => this.RaiseAndSetIfChanged(ref _opacity, value);
    }

    // Selection State
    public SelectionManager SelectionManager { get; } = new SelectionManager();
    public ReadOnlyObservableCollection<IDrawableElement> SelectedElements => SelectionManager.Selected;

    // Internal Clipboard
    private IEnumerable<IDrawableElement> _internalClipboard = new List<IDrawableElement>();

    // New Snapshot History
    private readonly HistoryManager _historyManager;
    public SKRect CanvasSize { get; set; }

    // OAPH properties for command states
    private readonly ObservableAsPropertyHelper<bool> _canDelete;
    public bool CanDelete => _canDelete.Value;

    private readonly ObservableAsPropertyHelper<bool> _canGroup;
    public bool CanGroup => _canGroup.Value;

    private readonly ObservableAsPropertyHelper<bool> _canUngroup;
    public bool CanUngroup => _canUngroup.Value;

    private readonly ObservableAsPropertyHelper<bool> _canUndo;
    public bool CanUndo => _canUndo.Value;

    private readonly ObservableAsPropertyHelper<bool> _canRedo;
    public bool CanRedo => _canRedo.Value;

    private readonly ObservableAsPropertyHelper<bool> _canPaste;
    public bool CanPaste => _canPaste.Value;

    // Commands
    public ReactiveCommand<IDrawingTool, Unit> SelectToolCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelectedCommand { get; }
    public ReactiveCommand<Unit, Unit> GroupSelectedCommand { get; }
    public ReactiveCommand<Unit, Unit> UngroupSelectedCommand { get; }
    public ReactiveCommand<Unit, Unit> CopyCommand { get; }
    public ReactiveCommand<Unit, Unit> CutCommand { get; }
    public ReactiveCommand<Unit, Unit> PasteCommand { get; }
    public ReactiveCommand<Unit, Unit> AddLayerCommand { get; }
    public ReactiveCommand<Layer, Unit> RemoveLayerCommand { get; }
    public ReactiveCommand<Unit, Unit> UndoCommand { get; }
    public ReactiveCommand<Unit, Unit> RedoCommand { get; }

    public MainViewModel()
    {
      _historyManager = new HistoryManager();

      // Initialize available tools
      AvailableTools =
      [
        new SelectTool(),
        new FreehandTool(),
        new RectangleTool(),
        new EllipseTool(),
        new LineTool(),
        new FillTool(),
        new EraserBrushTool()
      ];

      // Initialize with a default layer
      var initialLayer = new Layer { Name = "Layer 1" };
      Layers.Add(initialLayer);
      CurrentLayer = initialLayer;

      // Initialize OAPH properties for command states
      _canDelete = this.WhenAnyValue(x => x.SelectedElements.Count)
        .Select(count => count > 0)
        .ToProperty(this, x => x.CanDelete);

      _canGroup = this.WhenAnyValue(x => x.SelectedElements.Count)
        .Select(count => count > 1)
        .ToProperty(this, x => x.CanGroup);

      _canUngroup = this.WhenAnyValue(x => x.SelectedElements.Count)
        .Select(count => count == 1 && SelectedElements.FirstOrDefault() is DrawableGroup)
        .ToProperty(this, x => x.CanUngroup);

      _canUndo = this.WhenAnyValue(x => x._historyManager.CanUndo)
        .ToProperty(this, x => x.CanUndo);

      _canRedo = this.WhenAnyValue(x => x._historyManager.CanRedo)
        .ToProperty(this, x => x.CanRedo);

      _canPaste = this.WhenAnyValue(x => x._internalClipboard)
        .Select(clipboard => clipboard?.Any() == true)
        .ToProperty(this, x => x.CanPaste);

      // Initialize commands using OAPH properties
      SelectToolCommand = ReactiveCommand.Create<IDrawingTool>(tool =>
      {
        ActiveTool = tool;
        MessageBus.Current.SendMessage(new ToolChangedMessage(tool));
      }, outputScheduler: RxApp.MainThreadScheduler);

      DeleteSelectedCommand = ReactiveCommand.Create(() =>
      {
        if (CurrentLayer is null || !SelectedElements.Any()) return;

        var elementsToRemove = SelectedElements.ToList();
        foreach (var element in elementsToRemove)
        {
          CurrentLayer.Elements.Remove(element);
        }
        SelectionManager.Clear();
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        SaveState();
      }, this.WhenAnyValue(x => x.CanDelete), RxApp.MainThreadScheduler);

      GroupSelectedCommand = ReactiveCommand.Create(() =>
      {
        if (CurrentLayer is null || !SelectedElements.Any()) return;

        var elementsToGroup = SelectedElements.ToList();
        var group = new DrawableGroup();

        foreach (var element in elementsToGroup)
        {
          CurrentLayer.Elements.Remove(element);
          group.Children.Add(element);
        }
        CurrentLayer.Elements.Add(group);
        SelectionManager.Clear();
        SelectionManager.Add(group);
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        SaveState();
      }, this.WhenAnyValue(x => x.CanGroup), RxApp.MainThreadScheduler);

      UngroupSelectedCommand = ReactiveCommand.Create(() =>
      {
        if (CurrentLayer is null) return;
        var group = SelectedElements.First() as DrawableGroup;
        if (group != null)
        {
          CurrentLayer.Elements.Remove(group);
          foreach (var child in group.Children)
          {
            CurrentLayer.Elements.Add(child);
          }
          SelectionManager.Clear();
          MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
          SaveState();
        }
      }, this.WhenAnyValue(x => x.CanUngroup), RxApp.MainThreadScheduler);

      CopyCommand = ReactiveCommand.Create(() =>
      {
        _internalClipboard = SelectedElements.Select(e => e.Clone()).ToList();
      }, this.WhenAnyValue(x => x.CanDelete), RxApp.MainThreadScheduler);

      CutCommand = ReactiveCommand.Create(() =>
      {
        if (CurrentLayer is null || !SelectedElements.Any()) return;
        _internalClipboard = SelectedElements.Select(e => e.Clone()).ToList();
        var elementsToRemove = SelectedElements.ToList();
        foreach (var element in elementsToRemove)
        {
          CurrentLayer.Elements.Remove(element);
        }
        SelectionManager.Clear();
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        SaveState();
      }, this.WhenAnyValue(x => x.CanDelete), RxApp.MainThreadScheduler);

      PasteCommand = ReactiveCommand.Create(() =>
      {
        if (CurrentLayer is null || !_internalClipboard.Any()) return;
        foreach (var element in _internalClipboard)
        {
          var clone = element.Clone();
          clone.Translate(new SKPoint(10, 10)); // Offset pasted element
          CurrentLayer.Elements.Add(clone);
        }
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        SaveState();
      }, this.WhenAnyValue(x => x.CanPaste), RxApp.MainThreadScheduler);

      AddLayerCommand = ReactiveCommand.Create(() =>
      {
        var newLayer = new Layer { Name = $"Layer {Layers.Count + 1}" };
        Layers.Add(newLayer);
        CurrentLayer = newLayer;
        SaveState();
      }, outputScheduler: RxApp.MainThreadScheduler);

      RemoveLayerCommand = ReactiveCommand.Create<Layer>(layer =>
      {
        if (Layers.Count > 1)
        {
          Layers.Remove(layer);
          CurrentLayer = Layers.First();
          SaveState();
        }
      }, outputScheduler: RxApp.MainThreadScheduler);

      UndoCommand = ReactiveCommand.Create(() =>
      {
        var state = _historyManager.Undo();
        if (state != null)
        {
            RestoreState(state);
        }
      }, this.WhenAnyValue(x => x.CanUndo), RxApp.MainThreadScheduler);

      RedoCommand = ReactiveCommand.Create(() =>
      {
        var state = _historyManager.Redo();
        if (state != null)
        {
            RestoreState(state);
        }
      }, this.WhenAnyValue(x => x.CanRedo), RxApp.MainThreadScheduler);

      // Message listener for tools to trigger a state save
      MessageBus.Current.Listen<DrawingStateChangedMessage>().Subscribe(_ => SaveState());

      // Message listener for brush settings changes
      MessageBus.Current.Listen<BrushSettingsChangedMessage>().Subscribe(msg =>
      {
        if (msg.StrokeColor.HasValue)
          StrokeColor = msg.StrokeColor.Value;
        if (msg.FillColor.HasValue)
          FillColor = msg.FillColor.Value;
        if (msg.Transparency.HasValue)
          Opacity = msg.Transparency.Value;
      });

      // Save initial state
      SaveState();
    }

    public void SaveState()
    {
      _historyManager.SaveState(Layers);
    }

    private void RestoreState(List<Layer> state)
    {
        Layers.Clear();
        foreach (var layer in state)
        {
            Layers.Add(layer);
        }
        
        // Try to find the previously selected layer by ID, or default to first
        var currentLayerId = CurrentLayer?.Id;
        CurrentLayer = Layers.FirstOrDefault(l => l.Id == currentLayerId) ?? Layers.FirstOrDefault();
        
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void ProcessTouch(SKTouchEventArgs e)
    {
      if (CurrentLayer == null) return;

      switch (e.ActionType)
      {
        case SKTouchAction.Pressed:
          _activeTouches[e.Id] = e.Location;
          break;
        case SKTouchAction.Released:
        case SKTouchAction.Cancelled:
          _activeTouches.Remove(e.Id);
          break;
      }

      // Handle Navigation (Multi-touch)
      if (_activeTouches.Count >= 2 && e.ActionType == SKTouchAction.Moved)
      {
        // Find the pivot (any other finger)
        var pivotId = _activeTouches.Keys.FirstOrDefault(k => k != e.Id);
        if (pivotId != 0 && _activeTouches.TryGetValue(pivotId, out var pivotPoint))
        {
          var prevPoint = _activeTouches[e.Id];
          var newPoint = e.Location;

          float rotation = 0;
          var touchMatrix = _touchManipulationManager.TwoFingerManipulate(prevPoint, newPoint, pivotPoint, ref rotation);

          // Apply transform
          NavigationModel.TotalMatrix = SKMatrix.Concat(touchMatrix, NavigationModel.TotalMatrix);
          
          // Update stored location
          _activeTouches[e.Id] = newPoint;
          
          MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
          return; // Do not pass to tools
        }
      }

      // Handle Drawing/Tools (Single touch)
      if (_activeTouches.Count <= 1)
      {
          // If we just released the last finger, we might want to let the tool know
          // But if we were navigating, we shouldn't draw. 
          // For now, simple logic: if 1 finger, pass to tool.

          // Transform point to World Coordinates
          SKMatrix inverse = SKMatrix.CreateIdentity();
          if (NavigationModel.TotalMatrix.TryInvert(out inverse))
          {
              var worldPoint = inverse.MapPoint(e.Location);

              var context = new ToolContext
              {
                  CurrentLayer = CurrentLayer,
                  StrokeColor = StrokeColor,
                  FillColor = FillColor,
                  StrokeWidth = StrokeWidth,
                  Opacity = Opacity,
                  AllElements = Layers.SelectMany(l => l.Elements),
                  SelectionManager = SelectionManager
              };

              switch (e.ActionType)
              {
                  case SKTouchAction.Pressed:
                      HandleTouchPressed(worldPoint, context);
                      break;
                  case SKTouchAction.Moved:
                      ActiveTool.OnTouchMoved(worldPoint, context);
                      // Update stored location
                      if (_activeTouches.ContainsKey(e.Id)) _activeTouches[e.Id] = e.Location; 
                      break;
                  case SKTouchAction.Released:
                      ActiveTool.OnTouchReleased(worldPoint, context);
                      break;
              }
          }
      }
      
      // Update stored location for Moved events if not handled by navigation
      if (e.ActionType == SKTouchAction.Moved && _activeTouches.ContainsKey(e.Id))
      {
           _activeTouches[e.Id] = e.Location;
      }
    }

    private void HandleTouchPressed(SKPoint point, ToolContext context)
    {
      // If we're using the select tool, let it handle all selection logic
      if (ActiveTool.Type == ToolType.Select)
      {
        ActiveTool.OnTouchPressed(point, context);
        return;
      }

      // For other tools, clear selection and proceed with drawing
      if (SelectedElements.Any())
      {
        SelectionManager.Clear();
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
      }

      // Pass to the active tool for drawing operations
      ActiveTool.OnTouchPressed(point, context);
    }
  }
}
