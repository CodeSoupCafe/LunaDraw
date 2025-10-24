using LunaDraw.Logic.Messages;
using LunaDraw.Logic.ViewModels;
using ReactiveUI;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LunaDraw.Pages;

public partial class MainPage : ContentPage
{
    private MainViewModel _viewModel;

    public MainPage()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        BindingContext = _viewModel;
        toolbarView.BindingContext = new ToolbarViewModel(_viewModel);

        MessageBus.Current.Listen<CanvasInvalidateMessage>().Subscribe(_ =>
        {
            canvasView?.InvalidateSurface();
        });
    }

    private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        SKSurface surface = e.Surface;
        SKCanvas canvas = surface.Canvas;

        canvas.Clear(SKColors.White);

        if (_viewModel != null)
        {
            foreach (var layer in _viewModel.Layers)
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

            _viewModel.ActiveTool.DrawPreview(canvas, _viewModel);
        }
    }

    private void OnTouch(object sender, SKTouchEventArgs e)
    {
        _viewModel?.ProcessTouch(e);
        e.Handled = true;
    }
}
