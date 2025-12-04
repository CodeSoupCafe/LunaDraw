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
        private readonly MainViewModel mainViewModel;
        private readonly IToolStateManager toolStateManager;
        private readonly IMessageBus messageBus;

        // Forward properties from MainViewModel or ToolState
        public List<IDrawingTool> AvailableTools => toolStateManager.AvailableTools;
        public List<BrushShape> AvailableBrushShapes => toolStateManager.AvailableBrushShapes;

        // Commands delegate to MainViewModel (for now, until commands are moved to services/viewmodels)
        public ReactiveCommand<IDrawingTool, Unit> SelectToolCommand => mainViewModel.SelectToolCommand;
        public ReactiveCommand<Unit, Unit> UndoCommand => mainViewModel.UndoCommand;
        public ReactiveCommand<Unit, Unit> RedoCommand => mainViewModel.RedoCommand;
        public ReactiveCommand<Unit, Unit> CopyCommand => mainViewModel.CopyCommand;
        public ReactiveCommand<Unit, Unit> PasteCommand => mainViewModel.PasteCommand;
        public ReactiveCommand<Unit, Unit> DeleteSelectedCommand => mainViewModel.DeleteSelectedCommand;
        public ReactiveCommand<Unit, Unit> GroupSelectedCommand => mainViewModel.GroupSelectedCommand;
        public ReactiveCommand<Unit, Unit> UngroupSelectedCommand => mainViewModel.UngroupSelectedCommand;

        // Local Commands
        public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowShapesFlyoutCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectRectangleCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectCircleCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectLineCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowBrushesFlyoutCommand { get; }
        public ReactiveCommand<BrushShape, Unit> SelectBrushShapeCommand { get; }

        // OAPH properties for reactive binding
        private IDrawingTool activeTool;
        public IDrawingTool ActiveTool
        {
            get => activeTool;
            set => this.RaiseAndSetIfChanged(ref activeTool, value);
        }

        private readonly ObservableAsPropertyHelper<SKColor> strokeColor;
        public SKColor StrokeColor => strokeColor.Value;

        private readonly ObservableAsPropertyHelper<SKColor?> fillColor;
        public SKColor? FillColor => fillColor.Value;

        private readonly ObservableAsPropertyHelper<float> strokeWidth;
        public float StrokeWidth => strokeWidth.Value;

        private readonly ObservableAsPropertyHelper<byte> opacity;
        public byte Opacity => opacity.Value;

        private readonly ObservableAsPropertyHelper<byte> flow;
        public byte Flow => flow.Value;

        private readonly ObservableAsPropertyHelper<float> spacing;
        public float Spacing => spacing.Value;

        private readonly ObservableAsPropertyHelper<BrushShape> currentBrushShape;
        public BrushShape CurrentBrushShape => currentBrushShape.Value;

        private readonly ObservableAsPropertyHelper<bool> isGlowEnabled;
        public bool IsGlowEnabled => isGlowEnabled.Value;

        private readonly ObservableAsPropertyHelper<SKColor> glowColor;
        public SKColor GlowColor => glowColor.Value;

        private readonly ObservableAsPropertyHelper<float> glowRadius;
        public float GlowRadius => glowRadius.Value;

        private readonly ObservableAsPropertyHelper<bool> isRainbowEnabled;
        public bool IsRainbowEnabled => isRainbowEnabled.Value;

        private readonly ObservableAsPropertyHelper<float> scatterRadius;
        public float ScatterRadius => scatterRadius.Value;

        private readonly ObservableAsPropertyHelper<float> sizeJitter;
        public float SizeJitter => sizeJitter.Value;

        private readonly ObservableAsPropertyHelper<float> angleJitter;
        public float AngleJitter => angleJitter.Value;

        private readonly ObservableAsPropertyHelper<float> hueJitter;
        public float HueJitter => hueJitter.Value;

        // UI state properties
        private bool isSettingsOpen = false;
        public bool IsSettingsOpen
        {
            get => isSettingsOpen;
            set => this.RaiseAndSetIfChanged(ref isSettingsOpen, value);
        }

        private bool isShapesFlyoutOpen = false;
        public bool IsShapesFlyoutOpen
        {
            get => isShapesFlyoutOpen;
            set => this.RaiseAndSetIfChanged(ref isShapesFlyoutOpen, value);
        }

        private bool isBrushesFlyoutOpen = false;
        public bool IsBrushesFlyoutOpen
        {
            get => isBrushesFlyoutOpen;
            set => this.RaiseAndSetIfChanged(ref isBrushesFlyoutOpen, value);
        }

        private readonly ObservableAsPropertyHelper<bool> isAnyFlyoutOpen;
        public bool IsAnyFlyoutOpen => isAnyFlyoutOpen.Value;

        private IDrawingTool lastActiveShapeTool;
        public IDrawingTool LastActiveShapeTool
        {
            get => lastActiveShapeTool;
            set => this.RaiseAndSetIfChanged(ref lastActiveShapeTool, value);
        }

        public ToolbarViewModel(MainViewModel mainViewModel, IToolStateManager toolStateManager, IMessageBus messageBus)
        {
            this.mainViewModel = mainViewModel;
            this.toolStateManager = toolStateManager;
            this.messageBus = messageBus;

            // Subscribe to ToolState changes via MainViewModel or directly?
            // Using MainViewModel properties to maintain consistency if they are wrapped there,
            // but better to use ToolState directly if possible.
            // However, since MainViewModel exposes the same instances, it should be fine.
            
            // Initialize ActiveTool and subscribe
            activeTool = this.toolStateManager.ActiveTool;
            
            // We subscribe to the ViewModel's property which is already synced with the Service
            // This ensures we are downstream of the MainViewModel's glue code if any exists.
            this.mainViewModel.WhenAnyValue(x => x.ActiveTool)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tool => ActiveTool = tool);

            strokeColor = this.mainViewModel.WhenAnyValue(x => x.StrokeColor)
              .ToProperty(this, x => x.StrokeColor, this.mainViewModel.StrokeColor);

            fillColor = this.mainViewModel.WhenAnyValue(x => x.FillColor)
              .ToProperty(this, x => x.FillColor, this.mainViewModel.FillColor);

            strokeWidth = this.mainViewModel.WhenAnyValue(x => x.StrokeWidth)
              .ToProperty(this, x => x.StrokeWidth, this.mainViewModel.StrokeWidth);

            opacity = this.mainViewModel.WhenAnyValue(x => x.Opacity)
              .ToProperty(this, x => x.Opacity, this.mainViewModel.Opacity);

            flow = this.mainViewModel.WhenAnyValue(x => x.Flow)
              .ToProperty(this, x => x.Flow, this.mainViewModel.Flow);

            spacing = this.mainViewModel.WhenAnyValue(x => x.Spacing)
              .ToProperty(this, x => x.Spacing, this.mainViewModel.Spacing);

            currentBrushShape = this.mainViewModel.WhenAnyValue(x => x.CurrentBrushShape)
              .ToProperty(this, x => x.CurrentBrushShape, this.mainViewModel.CurrentBrushShape);

            isGlowEnabled = this.mainViewModel.WhenAnyValue(x => x.IsGlowEnabled)
              .ToProperty(this, x => x.IsGlowEnabled, initialValue: this.mainViewModel.IsGlowEnabled);

            glowColor = this.mainViewModel.WhenAnyValue(x => x.GlowColor)
              .ToProperty(this, x => x.GlowColor, initialValue: this.mainViewModel.GlowColor);

            glowRadius = this.mainViewModel.WhenAnyValue(x => x.GlowRadius)
              .ToProperty(this, x => x.GlowRadius, initialValue: this.mainViewModel.GlowRadius);

            isRainbowEnabled = this.mainViewModel.WhenAnyValue(x => x.IsRainbowEnabled)
              .ToProperty(this, x => x.IsRainbowEnabled, initialValue: this.mainViewModel.IsRainbowEnabled);

            scatterRadius = this.mainViewModel.WhenAnyValue(x => x.ScatterRadius)
              .ToProperty(this, x => x.ScatterRadius, initialValue: this.mainViewModel.ScatterRadius);

            sizeJitter = this.mainViewModel.WhenAnyValue(x => x.SizeJitter)
              .ToProperty(this, x => x.SizeJitter, initialValue: this.mainViewModel.SizeJitter);

            angleJitter = this.mainViewModel.WhenAnyValue(x => x.AngleJitter)
              .ToProperty(this, x => x.AngleJitter, initialValue: this.mainViewModel.AngleJitter);

            hueJitter = this.mainViewModel.WhenAnyValue(x => x.HueJitter)
              .ToProperty(this, x => x.HueJitter, initialValue: this.mainViewModel.HueJitter);

            isAnyFlyoutOpen = this.WhenAnyValue(x => x.IsSettingsOpen, x => x.IsShapesFlyoutOpen, x => x.IsBrushesFlyoutOpen)
              .Select(values => values.Item1 || values.Item2 || values.Item3)
              .ToProperty(this, x => x.IsAnyFlyoutOpen);

            lastActiveShapeTool = AvailableTools.FirstOrDefault(t => t is RectangleTool)
                                   ?? AvailableTools.FirstOrDefault(t => t is EllipseTool)
                                   ?? AvailableTools.FirstOrDefault(t => t is LineTool)
                                   ?? new RectangleTool(messageBus);

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
                this.messageBus.SendMessage(new LunaDraw.Logic.Messages.BrushShapeChangedMessage(shape));
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
                var tool = AvailableTools.FirstOrDefault(t => t is RectangleTool) ?? new RectangleTool(messageBus);
                LastActiveShapeTool = tool;
                SelectToolCommand.Execute(tool).Subscribe();
                IsShapesFlyoutOpen = false;
            });

            SelectCircleCommand = ReactiveCommand.Create(() =>
            {
                var tool = AvailableTools.FirstOrDefault(t => t is EllipseTool) ?? new EllipseTool(messageBus);
                LastActiveShapeTool = tool;
                SelectToolCommand.Execute(tool).Subscribe();
                IsShapesFlyoutOpen = false;
            });

            SelectLineCommand = ReactiveCommand.Create(() =>
            {
                var tool = AvailableTools.FirstOrDefault(t => t is LineTool) ?? new LineTool(messageBus);
                LastActiveShapeTool = tool;
                SelectToolCommand.Execute(tool).Subscribe();
                IsShapesFlyoutOpen = false;
            });
        }
    }
}