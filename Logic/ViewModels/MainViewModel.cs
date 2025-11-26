using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Logic.ViewModels
{
  public class MainViewModel : ReactiveObject
  {
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
    private SKPicture? _currentSnapshot;
    public SKPicture? CurrentSnapshot
    {
      get => _currentSnapshot;
      private set => this.RaiseAndSetIfChanged(ref _currentSnapshot, value);
    }

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
        new EraserTool()
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

        SaveState();
        var elementsToRemove = SelectedElements.ToList();
        foreach (var element in elementsToRemove)
        {
          CurrentLayer.Elements.Remove(element);
        }
        SelectionManager.Clear();
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
      }, this.WhenAnyValue(x => x.CanDelete), RxApp.MainThreadScheduler);

      GroupSelectedCommand = ReactiveCommand.Create(() =>
      {
        if (CurrentLayer is null || !SelectedElements.Any()) return;

        SaveState();
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
      }, this.WhenAnyValue(x => x.CanGroup), RxApp.MainThreadScheduler);

      UngroupSelectedCommand = ReactiveCommand.Create(() =>
      {
        if (CurrentLayer is null) return;
        var group = SelectedElements.First() as DrawableGroup;
        if (group != null)
        {
          SaveState();
          CurrentLayer.Elements.Remove(group);
          foreach (var child in group.Children)
          {
            CurrentLayer.Elements.Add(child);
          }
          SelectionManager.Clear();
          MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }
      }, this.WhenAnyValue(x => x.CanUngroup), RxApp.MainThreadScheduler);

      CopyCommand = ReactiveCommand.Create(() =>
      {
        _internalClipboard = SelectedElements.Select(e => e.Clone()).ToList();
      }, this.WhenAnyValue(x => x.CanDelete), RxApp.MainThreadScheduler);

      CutCommand = ReactiveCommand.Create(() =>
      {
        if (CurrentLayer is null || !SelectedElements.Any()) return;
        SaveState();
        _internalClipboard = SelectedElements.Select(e => e.Clone()).ToList();
        var elementsToRemove = SelectedElements.ToList();
        foreach (var element in elementsToRemove)
        {
          CurrentLayer.Elements.Remove(element);
        }
        SelectionManager.Clear();
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
      }, this.WhenAnyValue(x => x.CanDelete), RxApp.MainThreadScheduler);

      PasteCommand = ReactiveCommand.Create(() =>
      {
        if (CurrentLayer is null || !_internalClipboard.Any()) return;
        SaveState();
        foreach (var element in _internalClipboard)
        {
          var clone = element.Clone();
          clone.Translate(new SKPoint(10, 10)); // Offset pasted element
          CurrentLayer.Elements.Add(clone);
        }
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
      }, this.WhenAnyValue(x => x.CanPaste), RxApp.MainThreadScheduler);

      AddLayerCommand = ReactiveCommand.Create(() =>
      {
        SaveState();
        var newLayer = new Layer { Name = $"Layer {Layers.Count + 1}" };
        Layers.Add(newLayer);
        CurrentLayer = newLayer;
      }, outputScheduler: RxApp.MainThreadScheduler);

      RemoveLayerCommand = ReactiveCommand.Create<Layer>(layer =>
      {
        if (Layers.Count > 1)
        {
          SaveState();
          Layers.Remove(layer);
          CurrentLayer = Layers.First();
        }
      }, outputScheduler: RxApp.MainThreadScheduler);

      UndoCommand = ReactiveCommand.Create(() =>
      {
        CurrentSnapshot = _historyManager.Undo();
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
      }, this.WhenAnyValue(x => x.CanUndo), RxApp.MainThreadScheduler);

      RedoCommand = ReactiveCommand.Create(() =>
      {
        CurrentSnapshot = _historyManager.Redo();
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
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
    }

    public void SaveState()
    {
      _historyManager.SaveSnapshot(Layers, CanvasSize);
      UpdateHistoryButtons();
    }

    public void UpdateHistoryButtons()
    {
      this.RaisePropertyChanged(nameof(UndoCommand));
      this.RaisePropertyChanged(nameof(RedoCommand));
    }

    public void ProcessTouch(SKTouchEventArgs e)
    {
      if (CurrentLayer == null) return;

      // Any touch action clears the snapshot, reverting to live drawing
      if (e.ActionType == SKTouchAction.Pressed)
      {
        if (CurrentSnapshot != null)
        {
          CurrentSnapshot = null;
          MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }
      }

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
          HandleTouchPressed(e.Location, context);
          break;
        case SKTouchAction.Moved:
          ActiveTool.OnTouchMoved(e.Location, context);
          break;
        case SKTouchAction.Released:
          ActiveTool.OnTouchReleased(e.Location, context);
          break;
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
