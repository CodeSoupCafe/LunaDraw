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

    // OAPH properties for reactive binding to MainViewModel
    private readonly ObservableAsPropertyHelper<SKColor> _strokeColor;
    public SKColor StrokeColor => _strokeColor.Value;

    private readonly ObservableAsPropertyHelper<SKColor?> _fillColor;
    public SKColor? FillColor => _fillColor.Value;

    private readonly ObservableAsPropertyHelper<float> _strokeWidth;
    public float StrokeWidth => _strokeWidth.Value;

    private readonly ObservableAsPropertyHelper<byte> _opacity;
    public byte Opacity => _opacity.Value;

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

    // Derived property using OAPH
    private readonly ObservableAsPropertyHelper<bool> _isAnyFlyoutOpen;
    public bool IsAnyFlyoutOpen => _isAnyFlyoutOpen.Value;

    public ToolbarViewModel(MainViewModel mainViewModel)
    {
      _mainViewModel = mainViewModel;

      // Initialize OAPH properties to observe MainViewModel
      _strokeColor = _mainViewModel.WhenAnyValue(x => x.StrokeColor)
        .ToProperty(this, x => x.StrokeColor);

      _fillColor = _mainViewModel.WhenAnyValue(x => x.FillColor)
        .ToProperty(this, x => x.FillColor);

      _strokeWidth = _mainViewModel.WhenAnyValue(x => x.StrokeWidth)
        .ToProperty(this, x => x.StrokeWidth);

      _opacity = _mainViewModel.WhenAnyValue(x => x.Opacity)
        .ToProperty(this, x => x.Opacity);

      // Derived property for any flyout open
      _isAnyFlyoutOpen = this.WhenAnyValue(x => x.IsSettingsOpen, x => x.IsShapesFlyoutOpen)
        .Select(values => values.Item1 || values.Item2)
        .ToProperty(this, x => x.IsAnyFlyoutOpen);

      ShowShapesFlyoutCommand = ReactiveCommand.Create(() =>
      {
        // Close settings if open, then toggle shapes
        IsSettingsOpen = false;
        IsShapesFlyoutOpen = !IsShapesFlyoutOpen;
      });

      // Settings command — toggle settings and ensure shapes panel closed
      ShowSettingsCommand = ReactiveCommand.Create(() =>
      {
        IsSettingsOpen = !IsSettingsOpen;
        IsShapesFlyoutOpen = false;
      });

      SelectRectangleCommand = ReactiveCommand.Create(() =>
      {
        var tool = _mainViewModel.AvailableTools.FirstOrDefault(t => t is RectangleTool) ?? new RectangleTool();
        SelectToolCommand.Execute(tool).Subscribe();
        IsShapesFlyoutOpen = false;
      });

      SelectCircleCommand = ReactiveCommand.Create(() =>
      {
        var tool = _mainViewModel.AvailableTools.FirstOrDefault(t => t is EllipseTool) ?? new EllipseTool();
        SelectToolCommand.Execute(tool).Subscribe();
        IsShapesFlyoutOpen = false;
      });

      SelectLineCommand = ReactiveCommand.Create(() =>
      {
        var tool = _mainViewModel.AvailableTools.FirstOrDefault(t => t is LineTool) ?? new LineTool();
        SelectToolCommand.Execute(tool).Subscribe();
        IsShapesFlyoutOpen = false;
      });
    }
  }
}
