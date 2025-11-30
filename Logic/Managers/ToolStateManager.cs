using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using ReactiveUI;
using SkiaSharp;

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

    private bool _isGlowEnabled = false;
    public bool IsGlowEnabled
    {
      get => _isGlowEnabled;
      set => this.RaiseAndSetIfChanged(ref _isGlowEnabled, value);
    }

    private SKColor _glowColor = SKColors.Yellow; // Default glow color
    public SKColor GlowColor
    {
      get => _glowColor;
      set => this.RaiseAndSetIfChanged(ref _glowColor, value);
    }

    private float _glowRadius = 10f; // Default glow radius
    public float GlowRadius
    {
      get => _glowRadius;
      set => this.RaiseAndSetIfChanged(ref _glowRadius, value);
    }

    private bool _isRainbowEnabled;
    public bool IsRainbowEnabled
    {
        get => _isRainbowEnabled;
        set => this.RaiseAndSetIfChanged(ref _isRainbowEnabled, value);
    }

    private float _scatterRadius;
    public float ScatterRadius
    {
        get => _scatterRadius;
        set => this.RaiseAndSetIfChanged(ref _scatterRadius, value);
    }

    private float _sizeJitter;
    public float SizeJitter
    {
        get => _sizeJitter;
        set => this.RaiseAndSetIfChanged(ref _sizeJitter, value);
    }

    private float _angleJitter;
    public float AngleJitter
    {
        get => _angleJitter;
        set => this.RaiseAndSetIfChanged(ref _angleJitter, value);
    }

    private float _hueJitter;
    public float HueJitter
    {
        get => _hueJitter;
        set => this.RaiseAndSetIfChanged(ref _hueJitter, value);
    }

    public List<IDrawingTool> AvailableTools { get; }
    public List<BrushShape> AvailableBrushShapes { get; }

    private readonly IMessageBus _messageBus;

    public ToolStateManager(IMessageBus messageBus)
    {
      _messageBus = messageBus;
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

      AvailableBrushShapes =
      [
          BrushShape.Circle(),
          BrushShape.Square(),
          BrushShape.Star(),
          BrushShape.Heart(),
          BrushShape.Sparkle(),
          BrushShape.Cloud(),
          BrushShape.Moon(),
          BrushShape.Lightning(),
          BrushShape.Diamond(),
          BrushShape.Triangle(),
          BrushShape.Hexagon()
      ];

      _activeTool = new FreehandTool();
      _currentBrushShape = AvailableBrushShapes.First();

      // Listen for messages that update tool state
      _messageBus.Listen<BrushSettingsChangedMessage>().Subscribe(msg =>
      {
        if (msg.StrokeColor.HasValue) StrokeColor = msg.StrokeColor.Value;
        if (msg.ShouldClearFillColor) FillColor = null;
        else if (msg.FillColor.HasValue) FillColor = msg.FillColor.Value;
        if (msg.Transparency.HasValue) Opacity = msg.Transparency.Value;
        if (msg.Flow.HasValue) Flow = msg.Flow.Value;
        if (msg.Spacing.HasValue) Spacing = msg.Spacing.Value;
        if (msg.StrokeWidth.HasValue) StrokeWidth = msg.StrokeWidth.Value;
        if (msg.IsGlowEnabled.HasValue) IsGlowEnabled = msg.IsGlowEnabled.Value;
        if (msg.GlowColor.HasValue) GlowColor = msg.GlowColor.Value;
        if (msg.GlowRadius.HasValue) GlowRadius = msg.GlowRadius.Value;
        if (msg.IsRainbowEnabled.HasValue) IsRainbowEnabled = msg.IsRainbowEnabled.Value;
        if (msg.ScatterRadius.HasValue) ScatterRadius = msg.ScatterRadius.Value;
        if (msg.SizeJitter.HasValue) SizeJitter = msg.SizeJitter.Value;
        if (msg.AngleJitter.HasValue) AngleJitter = msg.AngleJitter.Value;
        if (msg.HueJitter.HasValue) HueJitter = msg.HueJitter.Value;
      });

      _messageBus.Listen<BrushShapeChangedMessage>().Subscribe(msg =>
      {
        CurrentBrushShape = msg.Shape;
      });
    }
  }
}
