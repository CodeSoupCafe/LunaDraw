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
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LunaDraw.Components;

/// <summary>
/// Displays a live preview of the current brush stroke with all effects applied.
/// Shows a predefined S-curve with the current brush shape, color, size, and effects.
/// </summary>
public class BrushStrokePreviewControl : SKCanvasView
{
  // Bindable Properties

  public static readonly BindableProperty StrokeColorProperty =
    BindableProperty.Create(nameof(StrokeColor), typeof(SKColor), typeof(BrushStrokePreviewControl), SKColors.White, propertyChanged: OnPropertyChanged);

  public SKColor StrokeColor
  {
    get => (SKColor)GetValue(StrokeColorProperty);
    set => SetValue(StrokeColorProperty, value);
  }

  public static readonly BindableProperty StrokeWidthProperty =
    BindableProperty.Create(nameof(StrokeWidth), typeof(float), typeof(BrushStrokePreviewControl), 10f, propertyChanged: OnPropertyChanged);

  public float StrokeWidth
  {
    get => (float)GetValue(StrokeWidthProperty);
    set => SetValue(StrokeWidthProperty, value);
  }

  public static readonly BindableProperty BrushShapeProperty =
    BindableProperty.Create(nameof(BrushShape), typeof(BrushShape), typeof(BrushStrokePreviewControl), null, propertyChanged: OnPropertyChanged);

  public BrushShape BrushShape
  {
    get => (BrushShape)GetValue(BrushShapeProperty);
    set => SetValue(BrushShapeProperty, value);
  }

  public static readonly BindableProperty IsGlowEnabledProperty =
    BindableProperty.Create(nameof(IsGlowEnabled), typeof(bool), typeof(BrushStrokePreviewControl), false, propertyChanged: OnPropertyChanged);

  public bool IsGlowEnabled
  {
    get => (bool)GetValue(IsGlowEnabledProperty);
    set => SetValue(IsGlowEnabledProperty, value);
  }

  public static readonly BindableProperty GlowColorProperty =
    BindableProperty.Create(nameof(GlowColor), typeof(SKColor), typeof(BrushStrokePreviewControl), SKColors.Yellow, propertyChanged: OnPropertyChanged);

  public SKColor GlowColor
  {
    get => (SKColor)GetValue(GlowColorProperty);
    set => SetValue(GlowColorProperty, value);
  }

  public static readonly BindableProperty GlowRadiusProperty =
    BindableProperty.Create(nameof(GlowRadius), typeof(float), typeof(BrushStrokePreviewControl), 10f, propertyChanged: OnPropertyChanged);

  public float GlowRadius
  {
    get => (float)GetValue(GlowRadiusProperty);
    set => SetValue(GlowRadiusProperty, value);
  }

  public static readonly BindableProperty IsRainbowEnabledProperty =
    BindableProperty.Create(nameof(IsRainbowEnabled), typeof(bool), typeof(BrushStrokePreviewControl), false, propertyChanged: OnPropertyChanged);

  public bool IsRainbowEnabled
  {
    get => (bool)GetValue(IsRainbowEnabledProperty);
    set => SetValue(IsRainbowEnabledProperty, value);
  }

  public static readonly BindableProperty ScatterRadiusProperty =
    BindableProperty.Create(nameof(ScatterRadius), typeof(float), typeof(BrushStrokePreviewControl), 0f, propertyChanged: OnPropertyChanged);

  public float ScatterRadius
  {
    get => (float)GetValue(ScatterRadiusProperty);
    set => SetValue(ScatterRadiusProperty, value);
  }

  public static readonly BindableProperty SizeJitterProperty =
    BindableProperty.Create(nameof(SizeJitter), typeof(float), typeof(BrushStrokePreviewControl), 0f, propertyChanged: OnPropertyChanged);

  public float SizeJitter
  {
    get => (float)GetValue(SizeJitterProperty);
    set => SetValue(SizeJitterProperty, value);
  }

  public static readonly BindableProperty AngleJitterProperty =
    BindableProperty.Create(nameof(AngleJitter), typeof(float), typeof(BrushStrokePreviewControl), 0f, propertyChanged: OnPropertyChanged);

  public float AngleJitter
  {
    get => (float)GetValue(AngleJitterProperty);
    set => SetValue(AngleJitterProperty, value);
  }

  public static readonly BindableProperty HueJitterProperty =
    BindableProperty.Create(nameof(HueJitter), typeof(float), typeof(BrushStrokePreviewControl), 0f, propertyChanged: OnPropertyChanged);

  public float HueJitter
  {
    get => (float)GetValue(HueJitterProperty);
    set => SetValue(HueJitterProperty, value);
  }

  private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
  {
    if (bindable is BrushStrokePreviewControl control)
    {
      control.InvalidateSurface();
    }
  }

  protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
  {
    base.OnPaintSurface(e);

    var canvas = e.Surface.Canvas;
    canvas.Clear(SKColors.Transparent);

    var info = e.Info;

    // Create a smooth S-curve path
    using var path = new SKPath();

    // Start point (left)
    float startX = info.Width * 0.1f;
    float startY = info.Height * 0.5f;

    // End point (right)
    float endX = info.Width * 0.9f;
    float endY = info.Height * 0.5f;

    // Control points for smooth S-curve
    float cp1X = info.Width * 0.3f;
    float cp1Y = info.Height * 0.2f;
    float cp2X = info.Width * 0.7f;
    float cp2Y = info.Height * 0.8f;

    path.MoveTo(startX, startY);
    path.CubicTo(cp1X, cp1Y, cp2X, cp2Y, endX, endY);

    // Draw the stroke with current settings
    using var paint = new SKPaint
    {
      IsAntialias = true,
      Style = SKPaintStyle.Stroke,
      StrokeWidth = StrokeWidth,
      Color = StrokeColor
    };

    // Apply glow effect if enabled
    if (IsGlowEnabled && GlowRadius > 0)
    {
      paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, GlowRadius / 2);
      paint.Color = GlowColor.WithAlpha((byte)(GlowColor.Alpha * 0.8f));
      canvas.DrawPath(path, paint);

      // Draw main stroke on top
      paint.MaskFilter = null;
      paint.Color = StrokeColor;
    }

    // Apply brush shape - always use round cap for preview simplicity
    paint.StrokeCap = SKStrokeCap.Round;

    canvas.DrawPath(path, paint);
  }
}
