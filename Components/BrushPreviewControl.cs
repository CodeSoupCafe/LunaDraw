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

namespace LunaDraw.Components
{
  public class BrushPreviewControl : SKCanvasView
  {
    public static readonly BindableProperty BrushShapeProperty =
        BindableProperty.Create(nameof(BrushShape), typeof(BrushShape), typeof(BrushPreviewControl), null, propertyChanged: OnPropertyChanged);

    public BrushShape BrushShape
    {
      get => (BrushShape)GetValue(BrushShapeProperty);
      set => SetValue(BrushShapeProperty, value);
    }

    public static readonly BindableProperty ColorProperty =
         BindableProperty.Create(nameof(Color), typeof(Color), typeof(BrushPreviewControl), Colors.Black, propertyChanged: OnPropertyChanged);

    public Color Color
    {
      get => (Color)GetValue(ColorProperty);
      set => SetValue(ColorProperty, value);
    }

    public static readonly BindableProperty StrokeColorProperty =
         BindableProperty.Create(nameof(StrokeColor), typeof(SKColor), typeof(BrushPreviewControl), SKColors.Empty, propertyChanged: OnPropertyChanged);

    public SKColor StrokeColor
    {
      get => (SKColor)GetValue(StrokeColorProperty);
      set => SetValue(StrokeColorProperty, value);
    }

    public static readonly BindableProperty FillColorProperty =
         BindableProperty.Create(nameof(FillColor), typeof(SKColor?), typeof(BrushPreviewControl), null, propertyChanged: OnPropertyChanged);

    public SKColor? FillColor
    {
      get => (SKColor?)GetValue(FillColorProperty);
      set => SetValue(FillColorProperty, value);
    }

    public static readonly BindableProperty IsGlowEnabledProperty =
        BindableProperty.Create(nameof(IsGlowEnabled), typeof(bool), typeof(BrushPreviewControl), false, propertyChanged: OnPropertyChanged);

    public bool IsGlowEnabled
    {
      get => (bool)GetValue(IsGlowEnabledProperty);
      set => SetValue(IsGlowEnabledProperty, value);
    }

    public static readonly BindableProperty GlowColorProperty =
        BindableProperty.Create(nameof(GlowColor), typeof(SKColor), typeof(BrushPreviewControl), SKColors.Yellow, propertyChanged: OnPropertyChanged);

    public SKColor GlowColor
    {
      get => (SKColor)GetValue(GlowColorProperty);
      set => SetValue(GlowColorProperty, value);
    }

    public static readonly BindableProperty GlowRadiusProperty =
        BindableProperty.Create(nameof(GlowRadius), typeof(float), typeof(BrushPreviewControl), 10f, propertyChanged: OnPropertyChanged);

    public float GlowRadius
    {
      get => (float)GetValue(GlowRadiusProperty);
      set => SetValue(GlowRadiusProperty, value);
    }

    private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
      ((BrushPreviewControl)bindable).InvalidateSurface();
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
      base.OnPaintSurface(e);

      var canvas = e.Surface.Canvas;
      canvas.Clear();

      if (BrushShape?.Path == null) return;

      var info = e.Info;
      var center = new SKPoint(info.Width / 2, info.Height / 2);

      // Calculate scale to fit in the view (with padding)
      var bounds = BrushShape.Path.TightBounds;
      float maxDim = Math.Max(bounds.Width, bounds.Height);
      if (maxDim <= 0) maxDim = 1;

      float availableSize = Math.Min(info.Width, info.Height) * 0.6f; // 60% padding
      float scale = availableSize / maxDim;

      SKColor drawColor;
      if (StrokeColor != SKColors.Empty)
      {
        drawColor = StrokeColor;
      }
      else
      {
        drawColor = new SKColor((byte)(Color.Red * 255), (byte)(Color.Green * 255), (byte)(Color.Blue * 255), (byte)(Color.Alpha * 255));
      }

      canvas.Save();
      canvas.Translate(center.X, center.Y);
      canvas.Scale(scale);
      // Center the shape itself (if not centered at 0,0)
      canvas.Translate(-bounds.MidX, -bounds.MidY);

      // Draw Glow if enabled
      if (IsGlowEnabled)
      {
          using var glowPaint = new SKPaint
          {
              Style = SKPaintStyle.StrokeAndFill,
              Color = GlowColor,
              IsAntialias = true,
              MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, GlowRadius)
          };
          canvas.DrawPath(BrushShape.Path, glowPaint);
      }

      using var paint = new SKPaint
      {
        Style = SKPaintStyle.Fill,
        Color = drawColor,
        IsAntialias = true
      };

      canvas.DrawPath(BrushShape.Path, paint);
      canvas.Restore();
    }
  }
}
