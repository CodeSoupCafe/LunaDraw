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

using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Tools;

public class LineTool(IMessageBus messageBus) : IDrawingTool
{
  public string Name => "Line";
  public ToolType Type => ToolType.Line;

  private SKPoint startPoint;
  private DrawableLine? currentLine;
  private readonly IMessageBus messageBus = messageBus;

  public void OnTouchPressed(SKPoint point, ToolContext context)
  {
    if (context.CurrentLayer?.IsLocked == true) return;

    startPoint = point;
    currentLine = new DrawableLine
    {
      StartPoint = SKPoint.Empty,
      EndPoint = SKPoint.Empty,
      TransformMatrix = SKMatrix.CreateTranslation(point.X, point.Y),
      StrokeColor = context.StrokeColor,
      StrokeWidth = context.StrokeWidth,
      Opacity = context.Opacity
    };
  }

  public void OnTouchMoved(SKPoint point, ToolContext context)
  {
    if (context.CurrentLayer?.IsLocked == true || currentLine == null) return;

    currentLine.EndPoint = point - startPoint;
    messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  public void OnTouchReleased(SKPoint point, ToolContext context)
  {
    if (context.CurrentLayer == null || context.CurrentLayer.IsLocked || currentLine == null) return;

    if (!currentLine.EndPoint.Equals(SKPoint.Empty))
    {
      context.CurrentLayer.Elements.Add(currentLine);
      messageBus.SendMessage(new DrawingStateChangedMessage());
    }

    currentLine = null;
    messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  public void OnTouchCancelled(ToolContext context)
  {
    currentLine = null;
    messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  public void DrawPreview(SKCanvas canvas, ToolContext context)
  {
    if (currentLine != null)
    {
      currentLine.Draw(canvas);
    }
  }
}