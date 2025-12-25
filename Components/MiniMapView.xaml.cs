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

using System.Reactive.Linq;

using LunaDraw.Logic.Extensions;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using LunaDraw.Logic.Storage;

namespace LunaDraw.Components;

public partial class MiniMapView : ContentView
{
  private readonly IMessageBus? messageBus;
  private readonly IPreferencesFacade? preferencesFacade;
  private MainViewModel? viewModel;
  private SKMatrix fitMatrix;
  private float density = 1.0f;

  public MiniMapView()
  {
    InitializeComponent();

    Loaded += (s, e) =>
    {
      messageBus?.Listen<CanvasInvalidateMessage>()
          .Throttle(TimeSpan.FromMilliseconds(30), RxApp.MainThreadScheduler)
          .Subscribe(_ => miniMapCanvas?.InvalidateSurface());
    };

    this.messageBus = Handler?.MauiContext?.Services.GetService<IMessageBus>()
                 ?? IPlatformApplication.Current?.Services.GetService<IMessageBus>();
    this.preferencesFacade = Handler?.MauiContext?.Services.GetService<IPreferencesFacade>()
                 ?? IPlatformApplication.Current?.Services.GetService<IPreferencesFacade>();
  }

  protected override void OnBindingContextChanged()
  {
    base.OnBindingContextChanged();
    viewModel = BindingContext as MainViewModel;
    miniMapCanvas?.InvalidateSurface();
  }

  private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
  {
    if (viewModel == null) return;

    var canvas = e.Surface.Canvas;
    var info = e.Info;

    if (sender is SKCanvasView view && view.Width > 0)
    {
      density = (float)(info.Width / view.Width);
    }

    var bgColor = preferencesFacade?.GetCanvasBackgroundColor() ?? SKColors.White;
    canvas.Clear(bgColor);

    // Calculate bounds of all elements
    var contentBounds = SKRect.Empty;
    bool hasContent = false;

    foreach (var layer in viewModel.Layers)
    {
      if (!layer.IsVisible) continue;
      foreach (var element in layer.Elements)
      {
        if (!element.IsVisible) continue;
        var b = element.Bounds;
        if (hasContent)
          contentBounds.Union(b);
        else
        {
          contentBounds = b;
          hasContent = true;
        }
      }
    }

    if (!hasContent)
    {
      contentBounds = new SKRect(0, 0, 1000, 1000);
    }

    contentBounds.Inflate(50, 50);

    // Calculate fit matrix (world to minimap)
    float scaleX = info.Width / contentBounds.Width;
    float scaleY = info.Height / contentBounds.Height;
    float scale = Math.Min(scaleX, scaleY);

    float tx = (info.Width - contentBounds.Width * scale) / 2 - contentBounds.Left * scale;
    float ty = (info.Height - contentBounds.Height * scale) / 2 - contentBounds.Top * scale;

    fitMatrix = SKMatrix.CreateScale(scale, scale);
    fitMatrix = SKMatrix.Concat(SKMatrix.CreateTranslation(tx, ty), fitMatrix);

    // Draw content
    canvas.Save();
    canvas.Concat(fitMatrix);

    foreach (var layer in viewModel.Layers)
    {
      if (layer.IsVisible)
      {
        foreach (var element in layer.Elements)
        {
          if (element.IsVisible)
          {
            element.Draw(canvas);
          }
        }
      }
    }
    canvas.Restore();

    // Draw viewport indicator
    if (viewModel.NavigationModel.ViewMatrix.TryInvert(out var mainInverse))
    {
      var mainScreenRect = viewModel.CanvasSize;
      if (mainScreenRect.Width > 0)
      {
        // Map screen corners to world points
        var tl = mainInverse.MapPoint(new SKPoint(mainScreenRect.Left, mainScreenRect.Top));
        var tr = mainInverse.MapPoint(new SKPoint(mainScreenRect.Right, mainScreenRect.Top));
        var br = mainInverse.MapPoint(new SKPoint(mainScreenRect.Right, mainScreenRect.Bottom));
        var bl = mainInverse.MapPoint(new SKPoint(mainScreenRect.Left, mainScreenRect.Bottom));

        // Map world points to minimap points
        var mTl = fitMatrix.MapPoint(tl);
        var mTr = fitMatrix.MapPoint(tr);
        var mBr = fitMatrix.MapPoint(br);
        var mBl = fitMatrix.MapPoint(bl);

        using var path = new SKPath();
        path.MoveTo(mTl);
        path.LineTo(mTr);
        path.LineTo(mBr);
        path.LineTo(mBl);
        path.Close();

        using var paint = new SKPaint
        {
          Style = SKPaintStyle.Stroke,
          Color = SKColors.Red,
          StrokeWidth = 2,
          IsAntialias = true
        };
        canvas.DrawPath(path, paint);

        using var fillPaint = new SKPaint
        {
          Style = SKPaintStyle.Fill,
          Color = SKColors.Red.WithAlpha(50)
        };
        canvas.DrawPath(path, fillPaint);
      }
    }
  }

  private void OnTouch(object? sender, SKTouchEventArgs e)
  {
    if (viewModel == null) return;

    if (!e.InContact) return;

    var canvasView = sender as SKCanvasView;
    if (canvasView == null) return;

    switch (e.ActionType)
    {
      case SKTouchAction.Pressed:
      case SKTouchAction.Moved:
        var touchPointPixels = e.Location;

        if (fitMatrix.TryInvert(out var inverseFit))
        {
          var worldPoint = inverseFit.MapPoint(touchPointPixels);

          // Calculate where this world point currently appears on screen
          var currentViewPoint = viewModel.NavigationModel.ViewMatrix.MapPoint(worldPoint);
          var screenCenter = new SKPoint(viewModel.CanvasSize.Width / 2, viewModel.CanvasSize.Height / 2);

          // Calculate the delta to center it
          var delta = screenCenter - currentViewPoint;

          // Apply translation to view matrix
          var translation = SKMatrix.CreateTranslation(delta.X, delta.Y);
          viewModel.NavigationModel.ViewMatrix = viewModel.NavigationModel.ViewMatrix.PostConcat(translation);

          messageBus?.SendMessage(new CanvasInvalidateMessage());
        }
        e.Handled = true;
        break;
    }
  }
}