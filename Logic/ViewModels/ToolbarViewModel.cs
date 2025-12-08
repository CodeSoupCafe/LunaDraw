using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Storage;

using LunaDraw.Logic.Models;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Tools;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.ViewModels
{
    public class ToolbarViewModel : ReactiveObject
    {
        private readonly IToolStateManager toolStateManager;
        private readonly ILayerStateManager layerStateManager;
        private readonly SelectionViewModel selectionVM;
        private readonly HistoryViewModel historyVM;
        private readonly IMessageBus messageBus;
        private readonly IBitmapCacheManager bitmapCacheManager;
        private readonly NavigationModel navigationModel;
        private readonly IFileSaver fileSaver;

        // Forward properties from ToolState
        public List<IDrawingTool> AvailableTools => toolStateManager.AvailableTools;
        public List<BrushShape> AvailableBrushShapes => toolStateManager.AvailableBrushShapes;

        // Delegated Commands
        public ReactiveCommand<IDrawingTool, Unit> SelectToolCommand { get; }
        public ReactiveCommand<Unit, Unit> UndoCommand => historyVM.UndoCommand;
        public ReactiveCommand<Unit, Unit> RedoCommand => historyVM.RedoCommand;
        public ReactiveCommand<Unit, Unit> CopyCommand => selectionVM.CopyCommand;
        public ReactiveCommand<Unit, Unit> PasteCommand => selectionVM.PasteCommand;
        public ReactiveCommand<Unit, Unit> DeleteSelectedCommand => selectionVM.DeleteSelectedCommand;
        public ReactiveCommand<Unit, Unit> GroupSelectedCommand => selectionVM.GroupSelectedCommand;
        public ReactiveCommand<Unit, Unit> UngroupSelectedCommand => selectionVM.UngroupSelectedCommand;

        // Local Commands
        public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowShapesFlyoutCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectRectangleCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectCircleCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectLineCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowBrushesFlyoutCommand { get; }
        public ReactiveCommand<BrushShape, Unit> SelectBrushShapeCommand { get; }
        public ReactiveCommand<Unit, Unit> ImportImageCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveImageCommand { get; }

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

        public ToolbarViewModel(
            IToolStateManager toolStateManager,
            ILayerStateManager layerStateManager,
            SelectionViewModel selectionVM,
            HistoryViewModel historyVM,
            IMessageBus messageBus,
            IBitmapCacheManager bitmapCacheManager,
            NavigationModel navigationModel,
            IFileSaver fileSaver)
        {
            this.toolStateManager = toolStateManager;
            this.layerStateManager = layerStateManager;
            this.selectionVM = selectionVM;
            this.historyVM = historyVM;
            this.messageBus = messageBus;
            this.bitmapCacheManager = bitmapCacheManager;
            this.navigationModel = navigationModel;
            this.fileSaver = fileSaver;

            // Initialize ActiveTool and subscribe
            activeTool = this.toolStateManager.ActiveTool;
            
            this.toolStateManager.WhenAnyValue(x => x.ActiveTool)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tool => ActiveTool = tool);

            // Create SelectToolCommand locally
            SelectToolCommand = ReactiveCommand.Create<IDrawingTool>(tool =>
            {
                this.toolStateManager.ActiveTool = tool;
            }, outputScheduler: RxApp.MainThreadScheduler);

            // Subscribe directly to IToolStateManager
            strokeColor = this.toolStateManager.WhenAnyValue(x => x.StrokeColor)
              .ToProperty(this, x => x.StrokeColor, this.toolStateManager.StrokeColor);

            fillColor = this.toolStateManager.WhenAnyValue(x => x.FillColor)
              .ToProperty(this, x => x.FillColor, this.toolStateManager.FillColor);

            strokeWidth = this.toolStateManager.WhenAnyValue(x => x.StrokeWidth)
              .ToProperty(this, x => x.StrokeWidth, this.toolStateManager.StrokeWidth);

            opacity = this.toolStateManager.WhenAnyValue(x => x.Opacity)
              .ToProperty(this, x => x.Opacity, this.toolStateManager.Opacity);

            flow = this.toolStateManager.WhenAnyValue(x => x.Flow)
              .ToProperty(this, x => x.Flow, this.toolStateManager.Flow);

            spacing = this.toolStateManager.WhenAnyValue(x => x.Spacing)
              .ToProperty(this, x => x.Spacing, this.toolStateManager.Spacing);

            currentBrushShape = this.toolStateManager.WhenAnyValue(x => x.CurrentBrushShape)
              .ToProperty(this, x => x.CurrentBrushShape, this.toolStateManager.CurrentBrushShape);

            isGlowEnabled = this.toolStateManager.WhenAnyValue(x => x.IsGlowEnabled)
              .ToProperty(this, x => x.IsGlowEnabled, initialValue: this.toolStateManager.IsGlowEnabled);

            glowColor = this.toolStateManager.WhenAnyValue(x => x.GlowColor)
              .ToProperty(this, x => x.GlowColor, initialValue: this.toolStateManager.GlowColor);

            glowRadius = this.toolStateManager.WhenAnyValue(x => x.GlowRadius)
              .ToProperty(this, x => x.GlowRadius, initialValue: this.toolStateManager.GlowRadius);

            isRainbowEnabled = this.toolStateManager.WhenAnyValue(x => x.IsRainbowEnabled)
              .ToProperty(this, x => x.IsRainbowEnabled, initialValue: this.toolStateManager.IsRainbowEnabled);

            scatterRadius = this.toolStateManager.WhenAnyValue(x => x.ScatterRadius)
              .ToProperty(this, x => x.ScatterRadius, initialValue: this.toolStateManager.ScatterRadius);

            sizeJitter = this.toolStateManager.WhenAnyValue(x => x.SizeJitter)
              .ToProperty(this, x => x.SizeJitter, initialValue: this.toolStateManager.SizeJitter);

            angleJitter = this.toolStateManager.WhenAnyValue(x => x.AngleJitter)
              .ToProperty(this, x => x.AngleJitter, initialValue: this.toolStateManager.AngleJitter);

            hueJitter = this.toolStateManager.WhenAnyValue(x => x.HueJitter)
              .ToProperty(this, x => x.HueJitter, initialValue: this.toolStateManager.HueJitter);

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

            ImportImageCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    var result = await FilePicker.Default.PickAsync(new PickOptions
                    {
                        PickerTitle = "Select an image to import",
                        FileTypes = FilePickerFileType.Images
                    });

                    if (result != null)
                    {
                        string path = result.FullPath;

                        // On platforms where FullPath is not available, copy to cache
                        if (string.IsNullOrEmpty(path))
                        {
                            path = Path.Combine(FileSystem.CacheDirectory, result.FileName);
                            using var sourceStream = await result.OpenReadAsync();
                            using var destStream = File.Create(path);
                            await sourceStream.CopyToAsync(destStream);
                        }

                        // Load with downsampling (max 2048x2048)
                        var bitmap = await this.bitmapCacheManager.GetBitmapAsync(path, 2048, 2048);
                        
                        if (bitmap != null)
                        {
                            var drawableImage = new DrawableImage(bitmap)
                            {
                                SourcePath = path
                            };
                            
                            this.layerStateManager.CurrentLayer?.Elements.Add(drawableImage);
                            this.messageBus.SendMessage(new CanvasInvalidateMessage());
                            this.layerStateManager.SaveState();
                        }
                    }
                }
                catch (Exception ex)
                {
                     System.Diagnostics.Debug.WriteLine($"Error importing image: {ex.Message}");
                }
            });

            SaveImageCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    if (this.navigationModel.CanvasWidth <= 0 || this.navigationModel.CanvasHeight <= 0)
                        return;

                    using var surface = SKSurface.Create(new SKImageInfo((int)this.navigationModel.CanvasWidth, (int)this.navigationModel.CanvasHeight));
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.White);

                    canvas.Save();

                    // Apply the view transformation matrix
                    canvas.SetMatrix(this.navigationModel.ViewMatrix);

                    // Draw layers with masking support
                    var layers = this.layerStateManager.Layers;
                    for (int i = 0; i < layers.Count; i++)
                    {
                        var layer = layers[i];
                        if (!layer.IsVisible) continue;

                        if (layer.MaskingMode == Logic.Models.MaskingMode.Clip)
                        {
                            layer.Draw(canvas);
                        }
                        else
                        {
                            // Check if next layers are clipping layers
                            bool hasClippingLayers = false;
                            int nextIndex = i + 1;
                            while (nextIndex < layers.Count && layers[nextIndex].MaskingMode == Logic.Models.MaskingMode.Clip)
                            {
                                if (layers[nextIndex].IsVisible) hasClippingLayers = true;
                                nextIndex++;
                            }

                            if (hasClippingLayers)
                            {
                                canvas.SaveLayer();
                                layer.Draw(canvas);

                                using (var paint = new SKPaint { BlendMode = SKBlendMode.SrcATop })
                                {
                                    for (int j = i + 1; j < layers.Count; j++)
                                    {
                                        var clipLayer = layers[j];
                                        if (clipLayer.MaskingMode != Logic.Models.MaskingMode.Clip) break;

                                        if (clipLayer.IsVisible)
                                        {
                                            canvas.SaveLayer(paint);
                                            clipLayer.Draw(canvas);
                                            canvas.Restore();
                                        }

                                        i = j;
                                    }
                                }

                                canvas.Restore();
                            }
                            else
                            {
                                layer.Draw(canvas);
                            }
                        }
                    }

                    canvas.Restore();

                    using var image = surface.Snapshot();
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    using var stream = data.AsStream();

                    var result = await this.fileSaver.SaveAsync("lunadraw_canvas.png", stream);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving image: {ex.Message}");
                }
            });
        }
    }
}
