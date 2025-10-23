using LunaDraw.Logic.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LunaDraw.Pages;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainViewModel();
    }

    private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        SKImageInfo info = e.Info;
        SKSurface surface = e.Surface;
        SKCanvas canvas = surface.Canvas;

        canvas.Clear(SKColors.White);

        var viewModel = BindingContext as MainViewModel;
        if (viewModel != null)
        {
            foreach (var path in viewModel.Paths)
            {
                using (var paint = new SKPaint())
                {
                    paint.Style = SKPaintStyle.Stroke;
                    paint.Color = path.Color;
                    paint.StrokeWidth = path.StrokeWidth;
                    canvas.DrawPath(path.Path, paint);
                }
            }
        }
    }

    private void OnTouch(object sender, SKTouchEventArgs e)
    {
        var viewModel = BindingContext as MainViewModel;
        viewModel?.ProcessTouch(e);
        (sender as SKCanvasView)?.InvalidateSurface();
        e.Handled = true;
    }
}
