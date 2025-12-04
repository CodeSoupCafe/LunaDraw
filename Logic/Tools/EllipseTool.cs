using LunaDraw.Logic.Models;
using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public class EllipseTool : ShapeTool<DrawableEllipse>
  {
    public override string Name => "Ellipse";
    public override ToolType Type => ToolType.Ellipse;

    public EllipseTool(IMessageBus messageBus) : base(messageBus)
    {
    }

    protected override DrawableEllipse CreateShape(ToolContext context)
    {
      return new DrawableEllipse
      {
        StrokeColor = context.StrokeColor,
        StrokeWidth = context.StrokeWidth,
        Opacity = context.Opacity,
        FillColor = context.FillColor
      };
    }

    protected override void UpdateShape(DrawableEllipse shape, SKRect bounds, SKMatrix transform)
    {
      shape.TransformMatrix = transform;
      shape.Oval = bounds;
    }

    protected override bool IsShapeValid(DrawableEllipse shape)
    {
      return shape.Oval.Width > 0 || shape.Oval.Height > 0;
    }
  }
}