using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

// For SKCanvasView

namespace LunaDraw.Logic.Services
{
  public interface ICanvasInputHandler
  {
    void ProcessTouch(SKTouchEventArgs e, SKRect canvasViewPort, SKCanvasView canvasView);
  }
}
