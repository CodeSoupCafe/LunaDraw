using ReactiveUI;
using SkiaSharp;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Messages;

namespace LunaDraw.Logic.Services
{
    public class ToolStateManager : ReactiveObject, IToolStateManager
    {
        private IDrawingTool _activeTool;
        public IDrawingTool ActiveTool
        {
            get => _activeTool;
            set
            {
                this.RaiseAndSetIfChanged(ref _activeTool, value);
                _messageBus.SendMessage(new ToolChangedMessage(value));
            }
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

        private byte _flow = 255;
        public byte Flow
        {
            get => _flow;
            set => this.RaiseAndSetIfChanged(ref _flow, value);
        }

        private float _spacing = 0.25f;
        public float Spacing
        {
            get => _spacing;
            set => this.RaiseAndSetIfChanged(ref _spacing, value);
        }

        private BrushShape _currentBrushShape;
        public BrushShape CurrentBrushShape
        {
            get => _currentBrushShape;
            set => this.RaiseAndSetIfChanged(ref _currentBrushShape, value);
        }

        public List<IDrawingTool> AvailableTools { get; }
        public List<BrushShape> AvailableBrushShapes { get; }

        private readonly IMessageBus _messageBus;

        public ToolStateManager(IMessageBus messageBus)
        {
            _messageBus = messageBus;
            AvailableTools = new List<IDrawingTool>
            {
                new SelectTool(),
                new FreehandTool(),
                new RectangleTool(),
                new EllipseTool(),
                new LineTool(),
                new FillTool(),
                new EraserBrushTool()
            };

            AvailableBrushShapes = new List<BrushShape>
            {
                BrushShape.Circle(),
                BrushShape.Square(),
                BrushShape.Star()
            };

            _activeTool = new FreehandTool();
            _currentBrushShape = AvailableBrushShapes.First();

            // Listen for messages that update tool state
            _messageBus.Listen<BrushSettingsChangedMessage>().Subscribe(msg =>
            {
                if (msg.StrokeColor.HasValue) StrokeColor = msg.StrokeColor.Value;
                if (msg.FillColor.HasValue) FillColor = msg.FillColor.Value;
                if (msg.Transparency.HasValue) Opacity = msg.Transparency.Value;
                if (msg.Flow.HasValue) Flow = msg.Flow.Value;
                if (msg.Spacing.HasValue) Spacing = msg.Spacing.Value;
                if (msg.StrokeWidth.HasValue) StrokeWidth = msg.StrokeWidth.Value;
            });

            _messageBus.Listen<BrushShapeChangedMessage>().Subscribe(msg =>
            {
                CurrentBrushShape = msg.Shape;
            });
        }
    }
}
