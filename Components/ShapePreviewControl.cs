using LunaDraw.Logic.Tools;

using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LunaDraw.Components
{
  public class ShapePreviewControl : SKCanvasView
  {
    public ShapePreviewControl()
    {
      Loaded += (s, e) => InvalidateSurface();
    }
    public static readonly BindableProperty ActiveToolProperty =
        BindableProperty.Create(nameof(ActiveTool), typeof(IDrawingTool), typeof(ShapePreviewControl), null, propertyChanged: OnPropertyChanged);

    public IDrawingTool ActiveTool
    {
      get => (IDrawingTool)GetValue(ActiveToolProperty);
      set => SetValue(ActiveToolProperty, value);
    }

    public static readonly BindableProperty ShapeNameProperty =
        BindableProperty.Create(nameof(ShapeName), typeof(string), typeof(ShapePreviewControl), null, propertyChanged: OnPropertyChanged);

    public string ShapeName
    {
      get => (string)GetValue(ShapeNameProperty);
      set => SetValue(ShapeNameProperty, value);
    }

    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(nameof(StrokeColor), typeof(SKColor), typeof(ShapePreviewControl), SKColors.Black, propertyChanged: OnPropertyChanged);

    public SKColor StrokeColor
    {
      get => (SKColor)GetValue(StrokeColorProperty);
      set => SetValue(StrokeColorProperty, value);
    }

    public static readonly BindableProperty FillColorProperty =
        BindableProperty.Create(nameof(FillColor), typeof(SKColor?), typeof(ShapePreviewControl), null, propertyChanged: OnPropertyChanged);

    public SKColor? FillColor
    {
      get => (SKColor?)GetValue(FillColorProperty);
      set => SetValue(FillColorProperty, value);
    }

    public static readonly BindableProperty StrokeWidthProperty =
        BindableProperty.Create(nameof(StrokeWidth), typeof(float), typeof(ShapePreviewControl), 5f, propertyChanged: OnPropertyChanged);

    public float StrokeWidth
    {
      get => (float)GetValue(StrokeWidthProperty);
      set => SetValue(StrokeWidthProperty, value);
    }

    private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
      ((ShapePreviewControl)bindable).InvalidateSurface();
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
      base.OnPaintSurface(e);

      var canvas = e.Surface.Canvas;
      canvas.Clear();

      if (ActiveTool == null && string.IsNullOrEmpty(ShapeName)) return;

      var info = e.Info;
      float width = info.Width;
      float height = info.Height;
      float padding = width * 0.2f;
      var rect = new SKRect(padding, padding, width - padding, height - padding);

      using var paint = new SKPaint
      {
        IsAntialias = true,
        Color = StrokeColor,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = Math.Min(StrokeWidth, width * 0.1f) // Limit stroke width for preview
      };

      if (FillColor.HasValue)
      {
        using var fillPaint = new SKPaint
        {
          IsAntialias = true,
          Color = FillColor.Value,
          Style = SKPaintStyle.Fill
        };

        if ((ActiveTool is RectangleTool) || ShapeName == "Rectangle")
        {
          canvas.DrawRect(rect, fillPaint);
        }
        else if ((ActiveTool is EllipseTool) || ShapeName == "Circle")
        {
          canvas.DrawOval(rect, fillPaint);
        }
      }

      if ((ActiveTool is RectangleTool) || ShapeName == "Rectangle")
      {
        canvas.DrawRect(rect, paint);
      }
      else if ((ActiveTool is EllipseTool) || ShapeName == "Circle")
      {
        canvas.DrawOval(rect, paint);
      }
      else if ((ActiveTool is LineTool) || ShapeName == "Line")
      {
        canvas.DrawLine(rect.Left, rect.Bottom, rect.Right, rect.Top, paint);
      }
    }
  }
}
