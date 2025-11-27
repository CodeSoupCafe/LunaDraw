using System.Reactive;
using System.Reactive.Linq;

using LunaDraw.Logic.Tools;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.ViewModels
{
  public class ToolbarViewModel : ReactiveObject
  {
    private readonly MainViewModel _mainViewModel;
    public List<IDrawingTool> AvailableTools => _mainViewModel.AvailableTools;

    public ReactiveCommand<IDrawingTool, Unit> SelectToolCommand => _mainViewModel.SelectToolCommand;
    public ReactiveCommand<Unit, Unit> UndoCommand => _mainViewModel.UndoCommand;
    public ReactiveCommand<Unit, Unit> RedoCommand => _mainViewModel.RedoCommand;
    public ReactiveCommand<Unit, Unit> CopyCommand => _mainViewModel.CopyCommand;
    public ReactiveCommand<Unit, Unit> PasteCommand => _mainViewModel.PasteCommand;
    public ReactiveCommand<Unit, Unit> DeleteSelectedCommand => _mainViewModel.DeleteSelectedCommand;
    public ReactiveCommand<Unit, Unit> GroupSelectedCommand => _mainViewModel.GroupSelectedCommand;
    public ReactiveCommand<Unit, Unit> UngroupSelectedCommand => _mainViewModel.UngroupSelectedCommand;
    public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowShapesFlyoutCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectRectangleCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectCircleCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectLineCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowBrushesFlyoutCommand { get; }
    public ReactiveCommand<LunaDraw.Logic.Models.BrushShape, Unit> SelectBrushShapeCommand { get; }

    public List<LunaDraw.Logic.Models.BrushShape> AvailableBrushShapes => _mainViewModel.AvailableBrushShapes;

    // OAPH properties for reactive binding to MainViewModel
    private IDrawingTool _activeTool;
    public IDrawingTool ActiveTool
    {
        get => _activeTool;
        set => this.RaiseAndSetIfChanged(ref _activeTool, value);
    }

    private readonly ObservableAsPropertyHelper<SKColor> _strokeColor;
    public SKColor StrokeColor => _strokeColor.Value;

    private readonly ObservableAsPropertyHelper<SKColor?> _fillColor;
    public SKColor? FillColor => _fillColor.Value;

    private readonly ObservableAsPropertyHelper<float> _strokeWidth;
    public float StrokeWidth => _strokeWidth.Value;

    private readonly ObservableAsPropertyHelper<byte> _opacity;
    public byte Opacity => _opacity.Value;

    private readonly ObservableAsPropertyHelper<byte> _flow;
    public byte Flow => _flow.Value;

    private readonly ObservableAsPropertyHelper<float> _spacing;
    public float Spacing => _spacing.Value;

    // UI state properties
    private bool _isSettingsOpen = false;
    public bool IsSettingsOpen
    {
      get => _isSettingsOpen;
      set => this.RaiseAndSetIfChanged(ref _isSettingsOpen, value);
    }

    private bool _isShapesFlyoutOpen = false;
    public bool IsShapesFlyoutOpen
    {
      get => _isShapesFlyoutOpen;
      set => this.RaiseAndSetIfChanged(ref _isShapesFlyoutOpen, value);
    }

    private bool _isBrushesFlyoutOpen = false;
    public bool IsBrushesFlyoutOpen
    {
      get => _isBrushesFlyoutOpen;
      set => this.RaiseAndSetIfChanged(ref _isBrushesFlyoutOpen, value);
    }

    // Derived property using OAPH
    private readonly ObservableAsPropertyHelper<bool> _isAnyFlyoutOpen;
    public bool IsAnyFlyoutOpen => _isAnyFlyoutOpen.Value;

    private IDrawingTool _lastActiveShapeTool;
    public IDrawingTool LastActiveShapeTool
    {
        get => _lastActiveShapeTool;
        set => this.RaiseAndSetIfChanged(ref _lastActiveShapeTool, value);
    }

    public ToolbarViewModel(MainViewModel mainViewModel)
    {
      _mainViewModel = mainViewModel;

      // Initialize ActiveTool and subscribe to changes
      _activeTool = _mainViewModel.ActiveTool;
      _mainViewModel.WhenAnyValue(x => x.ActiveTool)
          .ObserveOn(RxApp.MainThreadScheduler)
          .Subscribe(tool => ActiveTool = tool);

      _strokeColor = _mainViewModel.WhenAnyValue(x => x.StrokeColor)
        .ToProperty(this, x => x.StrokeColor);

      _fillColor = _mainViewModel.WhenAnyValue(x => x.FillColor)
        .ToProperty(this, x => x.FillColor);

      _strokeWidth = _mainViewModel.WhenAnyValue(x => x.StrokeWidth)
        .ToProperty(this, x => x.StrokeWidth);

      _opacity = _mainViewModel.WhenAnyValue(x => x.Opacity)
        .ToProperty(this, x => x.Opacity);

      _flow = _mainViewModel.WhenAnyValue(x => x.Flow)
        .ToProperty(this, x => x.Flow);

      _spacing = _mainViewModel.WhenAnyValue(x => x.Spacing)
        .ToProperty(this, x => x.Spacing);

      // Derived property for any flyout open
      _isAnyFlyoutOpen = this.WhenAnyValue(x => x.IsSettingsOpen, x => x.IsShapesFlyoutOpen, x => x.IsBrushesFlyoutOpen)
        .Select(values => values.Item1 || values.Item2 || values.Item3)
        .ToProperty(this, x => x.IsAnyFlyoutOpen);

      // Initialize last active shape (default to Rectangle or first available shape)
      _lastActiveShapeTool = _mainViewModel.AvailableTools.FirstOrDefault(t => t is RectangleTool) 
                             ?? _mainViewModel.AvailableTools.FirstOrDefault(t => t is EllipseTool)
                             ?? _mainViewModel.AvailableTools.FirstOrDefault(t => t is LineTool)
                             ?? new RectangleTool();

      ShowShapesFlyoutCommand = ReactiveCommand.Create(() =>
      {
        // Close other flyouts
        IsSettingsOpen = false;
        IsBrushesFlyoutOpen = false;

        // If the active tool is ALREADY the last shape tool, toggle the flyout
        // (or if the flyout is already open, close it)
        if (ActiveTool == LastActiveShapeTool)
        {
            IsShapesFlyoutOpen = !IsShapesFlyoutOpen;
        }
        else
        {
            // Switch to the last shape tool and ensure flyout is closed
            SelectToolCommand.Execute(LastActiveShapeTool).Subscribe();
            IsShapesFlyoutOpen = false;
        }
      });

      ShowBrushesFlyoutCommand = ReactiveCommand.Create(() =>
      {
          IsSettingsOpen = false;
          IsShapesFlyoutOpen = false;
          
          var freehandTool = AvailableTools.FirstOrDefault(t => t.Type == ToolType.Freehand);
          
          if (ActiveTool == freehandTool)
          {
              IsBrushesFlyoutOpen = !IsBrushesFlyoutOpen;
          }
          else
          {
              if (freehandTool != null)
                  SelectToolCommand.Execute(freehandTool).Subscribe();
              IsBrushesFlyoutOpen = false;
          }
      });

      SelectBrushShapeCommand = ReactiveCommand.Create<LunaDraw.Logic.Models.BrushShape>(shape =>
      {
          // Use MessageBus to send update (similar to color picker)
          ReactiveUI.MessageBus.Current.SendMessage(new LunaDraw.Logic.Messages.BrushShapeChangedMessage(shape));
          IsBrushesFlyoutOpen = false;
          
          // Ensure tool is selected
          var freehandTool = AvailableTools.FirstOrDefault(t => t.Type == ToolType.Freehand);
          if (freehandTool != null && ActiveTool != freehandTool)
          {
              SelectToolCommand.Execute(freehandTool).Subscribe();
          }
      });

      // Settings command — toggle settings and ensure shapes panel closed
      ShowSettingsCommand = ReactiveCommand.Create(() =>
      {
        IsSettingsOpen = !IsSettingsOpen;
        IsShapesFlyoutOpen = false;
        IsBrushesFlyoutOpen = false;
      });

      SelectRectangleCommand = ReactiveCommand.Create(() =>
      {
        var tool = _mainViewModel.AvailableTools.FirstOrDefault(t => t is RectangleTool) ?? new RectangleTool();
        LastActiveShapeTool = tool;
        SelectToolCommand.Execute(tool).Subscribe();
        IsShapesFlyoutOpen = false;
      });

      SelectCircleCommand = ReactiveCommand.Create(() =>
      {
        var tool = _mainViewModel.AvailableTools.FirstOrDefault(t => t is EllipseTool) ?? new EllipseTool();
        LastActiveShapeTool = tool;
        SelectToolCommand.Execute(tool).Subscribe();
        IsShapesFlyoutOpen = false;
      });

      SelectLineCommand = ReactiveCommand.Create(() =>
      {
        var tool = _mainViewModel.AvailableTools.FirstOrDefault(t => t is LineTool) ?? new LineTool();
        LastActiveShapeTool = tool;
        SelectToolCommand.Execute(tool).Subscribe();
        IsShapesFlyoutOpen = false;
      });
    }
  }
}
