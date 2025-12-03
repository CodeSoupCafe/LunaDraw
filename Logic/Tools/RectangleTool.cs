using LunaDraw.Logic.Models;
using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public class RectangleTool : ShapeTool<DrawableRectangle>
  {
    public override string Name => "Rectangle";
    public override ToolType Type => ToolType.Rectangle;

    protected override DrawableRectangle CreateShape(ToolContext context)
    {
      return new DrawableRectangle
      {
        StrokeColor = context.StrokeColor,
        StrokeWidth = context.StrokeWidth,
        Opacity = context.Opacity,
        FillColor = context.FillColor
      };
    }

    protected override void UpdateShape(DrawableRectangle shape, SKRect bounds, SKMatrix transform)
    {
      shape.TransformMatrix = transform;
      shape.Rectangle = bounds;
    }

    protected override bool IsShapeValid(DrawableRectangle shape)
    {
      return shape.Rectangle.Width > 0 || shape.Rectangle.Height > 0;
    }
  }
}