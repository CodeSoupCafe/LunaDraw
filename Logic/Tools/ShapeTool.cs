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

using LunaDraw.Logic.Extensions;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Tools;

public abstract class ShapeTool<T>(IMessageBus messageBus) : IDrawingTool where T : class, IDrawableElement
{
  public abstract string Name { get; }
  public abstract ToolType Type { get; }

  protected readonly IMessageBus MessageBus = messageBus;
  protected SKPoint StartPoint;
  protected T? CurrentShape;

  protected abstract T CreateShape(ToolContext context);
  protected abstract void UpdateShape(T shape, SKRect bounds, SKMatrix transform);
  protected abstract bool IsShapeValid(T shape);

  public virtual void OnTouchPressed(SKPoint point, ToolContext context)
  {
    if (context.CurrentLayer?.IsLocked == true) return;

    StartPoint = point;
    CurrentShape = CreateShape(context);
  }

  public virtual void OnTouchMoved(SKPoint point, ToolContext context)
  {
    if (context.CurrentLayer?.IsLocked == true || CurrentShape == null) return;

    var (transform, bounds) = context.CanvasMatrix.CalculateRotatedBounds(StartPoint, point);
    UpdateShape(CurrentShape, bounds, transform);

    MessageBus.SendMessage(new CanvasInvalidateMessage());
  }

  public virtual void OnTouchReleased(SKPoint point, ToolContext context)
  {
    if (context.CurrentLayer == null || context.CurrentLayer.IsLocked || CurrentShape == null) return;

    if (IsShapeValid(CurrentShape))
    {
      context.CurrentLayer.Elements.Add(CurrentShape);
      MessageBus.SendMessage(new DrawingStateChangedMessage());
    }

    CurrentShape = null;
    MessageBus.SendMessage(new CanvasInvalidateMessage());
  }

  public virtual void OnTouchCancelled(ToolContext context)
  {
    CurrentShape = null;
    MessageBus.SendMessage(new CanvasInvalidateMessage());
  }

  public virtual void DrawPreview(SKCanvas canvas, ToolContext context)
  {
    if (CurrentShape != null)
    {
      CurrentShape.Draw(canvas);
    }
  }
}
