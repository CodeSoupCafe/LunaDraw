using LunaDraw.Logic.Messages;
using LunaDraw.Logic.ViewModels;
using ReactiveUI;
using System.Reactive.Linq;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LunaDraw.Pages;

public partial class MainPage : ContentPage
{
    private MainViewModel _viewModel;
    private ToolbarViewModel _toolbarViewModel;

    public MainPage()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        _toolbarViewModel = new ToolbarViewModel(_viewModel);
        BindingContext = _viewModel;
        toolbarView.BindingContext = _toolbarViewModel;

        // Set up flyout content binding contexts
        SettingsFlyoutContent.BindingContext = _toolbarViewModel;
        ShapesFlyoutContent.BindingContext = _toolbarViewModel;

        canvasView.Loaded += OnCanvasLoaded;
        canvasView.PaintSurface += OnCanvasViewPaintSurface;
        canvasView.Touch += OnTouch;

        MessageBus.Current.Listen<CanvasInvalidateMessage>().Subscribe(_ =>
        {
            canvasView?.InvalidateSurface();
        });

        // Subscribe to flyout visibility changes to show/hide overlay
        var settingsObs = _viewModel.WhenAnyValue(x => x.IsSettingsOpen);
        var shapesObs = _toolbarViewModel.WhenAnyValue(x => x.IsShapesFlyoutOpen);
        settingsObs.CombineLatest(shapesObs, (isSettingsOpen, isShapesOpen) => (isSettingsOpen, isShapesOpen))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tuple =>
                {
                    var (isSettingsOpen, isShapesOpen) = tuple;
                    var shouldShowOverlay = isSettingsOpen || isShapesOpen;
                    FlyoutOverlay.IsVisible = shouldShowOverlay;

                    // Ensure canvas can receive touch events when flyouts are closed
                    canvasView.InputTransparent = shouldShowOverlay;
                });
    }

    private void OnOverlayTapped(object sender, EventArgs e)
    {
        // Close any open flyouts when overlay is tapped
        _viewModel.IsSettingsOpen = false;
        _toolbarViewModel.IsShapesFlyoutOpen = false;
    }

    private void OnCanvasLoaded(object? sender, EventArgs e)
    {
        _viewModel.CanvasSize = new SKRect(0, 0, canvasView.CanvasSize.Width, canvasView.CanvasSize.Height);
        _viewModel.SaveState(); // Save the initial blank state
    }

    private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        SKSurface surface = e.Surface;
        SKCanvas canvas = surface.Canvas;

        canvas.Clear(SKColors.White);

        if (_viewModel == null) return;

        // If we are in a snapshot state (after undo/redo), draw the picture
        if (_viewModel.CurrentSnapshot != null)
        {
            canvas.DrawPicture(_viewModel.CurrentSnapshot);
        }
        else // Otherwise, draw the live elements from the layers
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
