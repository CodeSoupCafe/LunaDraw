/* 
 *  Copyright (c) 2025 CodeSoupCafe LLC
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 *  
 */

using LunaDraw.Logic.Models;
using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Tools;

public class RectangleTool(IMessageBus messageBus) : ShapeTool<DrawableRectangle>(messageBus)
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