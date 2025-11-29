using System.Reactive;
using System.Reactive.Linq;

using LunaDraw.Logic.Models;
using LunaDraw.Logic.Services;
using LunaDraw.Logic.Tools;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.ViewModels
{
    public class ToolbarViewModel : ReactiveObject
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IToolStateManager _toolStateManager;

        // Forward properties from MainViewModel or ToolState
        public List<IDrawingTool> AvailableTools => _toolStateManager.AvailableTools;
        public List<BrushShape> AvailableBrushShapes => _toolStateManager.AvailableBrushShapes;

        // Commands delegate to MainViewModel (for now, until commands are moved to services/viewmodels)
        public ReactiveCommand<IDrawingTool, Unit> SelectToolCommand => _mainViewModel.SelectToolCommand;
        public ReactiveCommand<Unit, Unit> UndoCommand => _mainViewModel.UndoCommand;
        public ReactiveCommand<Unit, Unit> RedoCommand => _mainViewModel.RedoCommand;
        public ReactiveCommand<Unit, Unit> CopyCommand => _mainViewModel.CopyCommand;
        public ReactiveCommand<Unit, Unit> PasteCommand => _mainViewModel.PasteCommand;
        public ReactiveCommand<Unit, Unit> DeleteSelectedCommand => _mainViewModel.DeleteSelectedCommand;
        public ReactiveCommand<Unit, Unit> GroupSelectedCommand => _mainViewModel.GroupSelectedCommand;
        public ReactiveCommand<Unit, Unit> UngroupSelectedCommand => _mainViewModel.UngroupSelectedCommand;

        // Local Commands
        public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowShapesFlyoutCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectRectangleCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectCircleCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectLineCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowBrushesFlyoutCommand { get; }
        public ReactiveCommand<BrushShape, Unit> SelectBrushShapeCommand { get; }

        // OAPH properties for reactive binding
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

        private readonly ObservableAsPropertyHelper<BrushShape> _currentBrushShape;
        public BrushShape CurrentBrushShape => _currentBrushShape.Value;

        private readonly ObservableAsPropertyHelper<bool> _isGlowEnabled;
        public bool IsGlowEnabled => _isGlowEnabled.Value;

        private readonly ObservableAsPropertyHelper<SKColor> _glowColor;
        public SKColor GlowColor => _glowColor.Value;

        private readonly ObservableAsPropertyHelper<float> _glowRadius;
        public float GlowRadius => _glowRadius.Value;

        private readonly ObservableAsPropertyHelper<bool> _isRainbowEnabled;
        public bool IsRainbowEnabled => _isRainbowEnabled.Value;

        private readonly ObservableAsPropertyHelper<float> _scatterRadius;
        public float ScatterRadius => _scatterRadius.Value;

        private readonly ObservableAsPropertyHelper<float> _sizeJitter;
        public float SizeJitter => _sizeJitter.Value;

        private readonly ObservableAsPropertyHelper<float> _angleJitter;
        public float AngleJitter => _angleJitter.Value;

        private readonly ObservableAsPropertyHelper<float> _hueJitter;
        public float HueJitter => _hueJitter.Value;

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

        private readonly ObservableAsPropertyHelper<bool> _isAnyFlyoutOpen;
        public bool IsAnyFlyoutOpen => _isAnyFlyoutOpen.Value;

        private IDrawingTool _lastActiveShapeTool;
        public IDrawingTool LastActiveShapeTool
        {
            get => _lastActiveShapeTool;
            set => this.RaiseAndSetIfChanged(ref _lastActiveShapeTool, value);
        }

        public ToolbarViewModel(MainViewModel mainViewModel, IToolStateManager toolStateManager)
        {
            _mainViewModel = mainViewModel;
            _toolStateManager = toolStateManager;

            // Subscribe to ToolState changes via MainViewModel or directly?
            // Using MainViewModel properties to maintain consistency if they are wrapped there,
            // but better to use ToolState directly if possible.
            // However, since MainViewModel exposes the same instances, it should be fine.
            
            // Initialize ActiveTool and subscribe
            _activeTool = _toolStateManager.ActiveTool;
            
            // We subscribe to the ViewModel's property which is already synced with the Service
            // This ensures we are downstream of the MainViewModel's glue code if any exists.
            _mainViewModel.WhenAnyValue(x => x.ActiveTool)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tool => ActiveTool = tool);

            _strokeColor = _mainViewModel.WhenAnyValue(x => x.StrokeColor)
              .ToProperty(this, x => x.StrokeColor, _mainViewModel.StrokeColor);

            _fillColor = _mainViewModel.WhenAnyValue(x => x.FillColor)
              .ToProperty(this, x => x.FillColor, _mainViewModel.FillColor);

            _strokeWidth = _mainViewModel.WhenAnyValue(x => x.StrokeWidth)
              .ToProperty(this, x => x.StrokeWidth, _mainViewModel.StrokeWidth);

            _opacity = _mainViewModel.WhenAnyValue(x => x.Opacity)
              .ToProperty(this, x => x.Opacity, _mainViewModel.Opacity);

            _flow = _mainViewModel.WhenAnyValue(x => x.Flow)
              .ToProperty(this, x => x.Flow, _mainViewModel.Flow);

            _spacing = _mainViewModel.WhenAnyValue(x => x.Spacing)
              .ToProperty(this, x => x.Spacing, _mainViewModel.Spacing);

            _currentBrushShape = _mainViewModel.WhenAnyValue(x => x.CurrentBrushShape)
              .ToProperty(this, x => x.CurrentBrushShape, _mainViewModel.CurrentBrushShape);

            _isGlowEnabled = _mainViewModel.WhenAnyValue(x => x.IsGlowEnabled)
              .ToProperty(this, x => x.IsGlowEnabled, initialValue: _mainViewModel.IsGlowEnabled);

            _glowColor = _mainViewModel.WhenAnyValue(x => x.GlowColor)
              .ToProperty(this, x => x.GlowColor, initialValue: _mainViewModel.GlowColor);

            _glowRadius = _mainViewModel.WhenAnyValue(x => x.GlowRadius)
              .ToProperty(this, x => x.GlowRadius, initialValue: _mainViewModel.GlowRadius);

            _isRainbowEnabled = _mainViewModel.WhenAnyValue(x => x.IsRainbowEnabled)
              .ToProperty(this, x => x.IsRainbowEnabled, initialValue: _mainViewModel.IsRainbowEnabled);

            _scatterRadius = _mainViewModel.WhenAnyValue(x => x.ScatterRadius)
              .ToProperty(this, x => x.ScatterRadius, initialValue: _mainViewModel.ScatterRadius);

            _sizeJitter = _mainViewModel.WhenAnyValue(x => x.SizeJitter)
              .ToProperty(this, x => x.SizeJitter, initialValue: _mainViewModel.SizeJitter);

            _angleJitter = _mainViewModel.WhenAnyValue(x => x.AngleJitter)
              .ToProperty(this, x => x.AngleJitter, initialValue: _mainViewModel.AngleJitter);

            _hueJitter = _mainViewModel.WhenAnyValue(x => x.HueJitter)
              .ToProperty(this, x => x.HueJitter, initialValue: _mainViewModel.HueJitter);

            _isAnyFlyoutOpen = this.WhenAnyValue(x => x.IsSettingsOpen, x => x.IsShapesFlyoutOpen, x => x.IsBrushesFlyoutOpen)
              .Select(values => values.Item1 || values.Item2 || values.Item3)
              .ToProperty(this, x => x.IsAnyFlyoutOpen);

            _lastActiveShapeTool = AvailableTools.FirstOrDefault(t => t is RectangleTool)
                                   ?? AvailableTools.FirstOrDefault(t => t is EllipseTool)
                                   ?? AvailableTools.FirstOrDefault(t => t is LineTool)
                                   ?? new RectangleTool();

            ShowShapesFlyoutCommand = ReactiveCommand.Create(() =>
            {
                IsSettingsOpen = false;
                IsBrushesFlyoutOpen = false;

                if (ActiveTool == LastActiveShapeTool)
                {
                    IsShapesFlyoutOpen = !IsShapesFlyoutOpen;
                }
                else
                {
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

            SelectBrushShapeCommand = ReactiveCommand.Create<BrushShape>(shape =>
            {
                ReactiveUI.MessageBus.Current.SendMessage(new LunaDraw.Logic.Messages.BrushShapeChangedMessage(shape));
                IsBrushesFlyoutOpen = false;

                var freehandTool = AvailableTools.FirstOrDefault(t => t.Type == ToolType.Freehand);
                if (freehandTool != null && ActiveTool != freehandTool)
                {
                    SelectToolCommand.Execute(freehandTool).Subscribe();
                }
            });

            ShowSettingsCommand = ReactiveCommand.Create(() =>
            {
                IsSettingsOpen = !IsSettingsOpen;
                IsShapesFlyoutOpen = false;
                IsBrushesFlyoutOpen = false;
            });

            SelectRectangleCommand = ReactiveCommand.Create(() =>
            {
                var tool = AvailableTools.FirstOrDefault(t => t is RectangleTool) ?? new RectangleTool();
                LastActiveShapeTool = tool;
                SelectToolCommand.Execute(tool).Subscribe();
                IsShapesFlyoutOpen = false;
            });

            SelectCircleCommand = ReactiveCommand.Create(() =>
            {
                var tool = AvailableTools.FirstOrDefault(t => t is EllipseTool) ?? new EllipseTool();
                LastActiveShapeTool = tool;
                SelectToolCommand.Execute(tool).Subscribe();
                IsShapesFlyoutOpen = false;
            });

            SelectLineCommand = ReactiveCommand.Create(() =>
            {
                var tool = AvailableTools.FirstOrDefault(t => t is LineTool) ?? new LineTool();
                LastActiveShapeTool = tool;
                SelectToolCommand.Execute(tool).Subscribe();
                IsShapesFlyoutOpen = false;
            });
        }
    }
}