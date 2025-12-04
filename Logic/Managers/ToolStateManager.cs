using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Services
{
  public class ToolStateManager : ReactiveObject, IToolStateManager
  {
    private IDrawingTool activeTool;
    public IDrawingTool ActiveTool
    {
      get => activeTool;
      set
      {
        this.RaiseAndSetIfChanged(ref activeTool, value);
        messageBus.SendMessage(new ToolChangedMessage(value));
      }
    }

    private SKColor strokeColor = SKColors.Black;
    public SKColor StrokeColor
    {
      get => strokeColor;
      set => this.RaiseAndSetIfChanged(ref strokeColor, value);
    }

    private SKColor? fillColor;
    public SKColor? FillColor
    {
      get => fillColor;
      set => this.RaiseAndSetIfChanged(ref fillColor, value);
    }

    private float strokeWidth = 5;
    public float StrokeWidth
    {
      get => strokeWidth;
      set => this.RaiseAndSetIfChanged(ref strokeWidth, value);
    }

    private byte opacity = 255;
    public byte Opacity
    {
      get => opacity;
      set => this.RaiseAndSetIfChanged(ref opacity, value);
    }

    private byte flow = 255;
    public byte Flow
    {
      get => flow;
      set => this.RaiseAndSetIfChanged(ref flow, value);
    }

    private float spacing = 0.25f;
    public float Spacing
    {
      get => spacing;
      set => this.RaiseAndSetIfChanged(ref spacing, value);
    }

    private BrushShape currentBrushShape;
    public BrushShape CurrentBrushShape
    {
      get => currentBrushShape;
      set => this.RaiseAndSetIfChanged(ref currentBrushShape, value);
    }

    private bool isGlowEnabled = false;
    public bool IsGlowEnabled
    {
      get => isGlowEnabled;
      set => this.RaiseAndSetIfChanged(ref isGlowEnabled, value);
    }

    private SKColor glowColor = SKColors.Yellow; // Default glow color
    public SKColor GlowColor
    {
      get => glowColor;
      set => this.RaiseAndSetIfChanged(ref glowColor, value);
    }

    private float glowRadius = 10f; // Default glow radius
    public float GlowRadius
    {
      get => glowRadius;
      set => this.RaiseAndSetIfChanged(ref glowRadius, value);
    }

    private bool isRainbowEnabled;
    public bool IsRainbowEnabled
    {
        get => isRainbowEnabled;
        set => this.RaiseAndSetIfChanged(ref isRainbowEnabled, value);
    }

    private float scatterRadius;
    public float ScatterRadius
    {
        get => scatterRadius;
        set => this.RaiseAndSetIfChanged(ref scatterRadius, value);
    }

    private float sizeJitter;
    public float SizeJitter
    {
        get => sizeJitter;
        set => this.RaiseAndSetIfChanged(ref sizeJitter, value);
    }

    private float angleJitter;
    public float AngleJitter
    {
        get => angleJitter;
        set => this.RaiseAndSetIfChanged(ref angleJitter, value);
    }

    private float hueJitter;
    public float HueJitter
    {
        get => hueJitter;
        set => this.RaiseAndSetIfChanged(ref hueJitter, value);
    }

    public List<IDrawingTool> AvailableTools { get; }
    public List<BrushShape> AvailableBrushShapes { get; }

    private readonly IMessageBus messageBus;

    public ToolStateManager(IMessageBus messageBus)
    {
      this.messageBus = messageBus;
      AvailableTools =
      [
          new SelectTool(messageBus),
                new FreehandTool(messageBus),
                new RectangleTool(messageBus),
                new EllipseTool(messageBus),
                new LineTool(messageBus),
                new FillTool(messageBus),
                new EraserBrushTool(messageBus)
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

      activeTool = new FreehandTool(messageBus);
      currentBrushShape = AvailableBrushShapes.First();

      // Listen for messages that update tool state
      this.messageBus.Listen<BrushSettingsChangedMessage>().Subscribe(msg =>
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

      this.messageBus.Listen<BrushShapeChangedMessage>().Subscribe(msg =>
      {
        CurrentBrushShape = msg.Shape;
      });
    }
  }
}