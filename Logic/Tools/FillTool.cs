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
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public class FillTool(IMessageBus messageBus) : IDrawingTool
  {
    public string Name => "Fill";
    public ToolType Type => ToolType.Fill;
    private readonly IMessageBus messageBus = messageBus;

        public void OnTouchPressed(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      var hitElement = context.AllElements
                              .Where(e => e.IsVisible)
                              .OrderByDescending(e => e.ZIndex)
                              .FirstOrDefault(e => e.HitTest(point));

      if (hitElement != null)
      {
        hitElement.FillColor = context.FillColor;
        messageBus.SendMessage(new CanvasInvalidateMessage());
        messageBus.SendMessage(new DrawingStateChangedMessage());
      }
    }

    public void OnTouchMoved(SKPoint point, ToolContext context)
    {
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
    }

    public void OnTouchCancelled(ToolContext context)
    {
    }

    public void DrawPreview(SKCanvas canvas, ToolContext context)
    {
    }
  }
}
