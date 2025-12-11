using LunaDraw.Logic.Messages;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Pages;

public partial class MainPage : ContentPage
{
  private readonly MainViewModel viewModel;
  private readonly ToolbarViewModel toolbarViewModel;
  private readonly IMessageBus messageBus;

  private MenuFlyout? canvasContextMenu;
  private MenuFlyoutSubItem? moveToLayerSubMenu;

  public MainPage(MainViewModel viewModel, ToolbarViewModel toolbarViewModel, IMessageBus messageBus)
  {
    InitializeComponent();
    this.viewModel = viewModel;
    this.toolbarViewModel = toolbarViewModel;
    this.messageBus = messageBus;

    BindingContext = this.viewModel;
    toolbarView.BindingContext = this.toolbarViewModel;

    InitializeContextMenu();

    SettingsFlyoutContent.BindingContext = this.toolbarViewModel;
    ShapesFlyoutContent.BindingContext = this.toolbarViewModel;
    BrushesFlyoutContent.BindingContext = this.toolbarViewModel;

    canvasView.PaintSurface += OnCanvasViewPaintSurface;
    canvasView.Touch += OnTouch;

    this.messageBus.Listen<CanvasInvalidateMessage>().Subscribe(_ =>
    {
      canvasView?.InvalidateSurface();
    });

    viewModel.SelectionManager.SelectionChanged += (s, e) => UpdateContextMenu();
    viewModel.Layers.CollectionChanged += (s, e) => UpdateContextMenu();
    UpdateContextMenu();
  }

  private void InitializeContextMenu()
  {
    canvasContextMenu = [];

    var arrangeSubMenu = new MenuFlyoutSubItem { Text = "Arrange" };

    var sendToBackItem = new MenuFlyoutItem { Text = "Send To Back" };
    sendToBackItem.SetBinding(MenuItem.CommandProperty, new Binding("SelectionVM.SendElementToBackCommand", source: viewModel));
    arrangeSubMenu.Add(sendToBackItem);

    var sendBackwardItem = new MenuFlyoutItem { Text = "Send Backward" };
    sendBackwardItem.SetBinding(MenuItem.CommandProperty, new Binding("SelectionVM.SendBackwardCommand", source: viewModel));
    arrangeSubMenu.Add(sendBackwardItem);

    var bringForwardItem = new MenuFlyoutItem { Text = "Bring Forward" };
    bringForwardItem.SetBinding(MenuItem.CommandProperty, new Binding("SelectionVM.BringForwardCommand", source: viewModel));
    arrangeSubMenu.Add(bringForwardItem);

    var sendToFrontItem = new MenuFlyoutItem { Text = "Send To Front" };
    sendToFrontItem.SetBinding(MenuItem.CommandProperty, new Binding("SelectionVM.BringElementToFrontCommand", source: viewModel));
    arrangeSubMenu.Add(sendToFrontItem);

    canvasContextMenu.Add(arrangeSubMenu);
    canvasContextMenu.Add(new MenuFlyoutSeparator());

    moveToLayerSubMenu = new MenuFlyoutSubItem { Text = "Move to" };

    canvasContextMenu.Add(moveToLayerSubMenu);

    FlyoutBase.SetContextFlyout(canvasView, canvasContextMenu);
  }

  private void UpdateContextMenu()
  {
    if (moveToLayerSubMenu == null) return;

    moveToLayerSubMenu.Clear();

    var addLayerItem = new MenuFlyoutItem { Text = "New Layer" };
    addLayerItem.SetBinding(MenuItem.CommandProperty, new Binding("SelectionVM.MoveSelectionToNewLayerCommand", source: viewModel));
    moveToLayerSubMenu.Add(addLayerItem);

    bool hasSelection = viewModel.SelectedElements.Any();
    moveToLayerSubMenu.IsEnabled = hasSelection;

    if (!hasSelection) return;

    foreach (var layer in viewModel.Layers)
    {
      var item = new MenuFlyoutItem
      {
        Text = layer.Name,
        Command = viewModel.SelectionVM.MoveSelectionToLayerCommand,
        CommandParameter = layer
      };
      moveToLayerSubMenu.Add(item);
    }
  }

  private void OnCanvasViewPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
  {
    SKSurface surface = e.Surface;
    SKCanvas canvas = surface.Canvas;

    int width = e.BackendRenderTarget.Width;
    int height = e.BackendRenderTarget.Height;
    viewModel.CanvasSize = new SKRect(0, 0, width, height);
    viewModel.NavigationModel.CanvasWidth = width;
    viewModel.NavigationModel.CanvasHeight = height;

    canvas.Clear(SKColors.White);

    if (viewModel == null) return;

    canvas.Save();

    // Apply the view transformation matrix
    canvas.SetMatrix(viewModel.NavigationModel.ViewMatrix);

    // Draw layers with masking support
    var layers = viewModel.Layers;
    for (int i = 0; i < layers.Count; i++)
    {
      var layer = layers[i];
      if (!layer.IsVisible) continue;

      if (layer.MaskingMode == Logic.Models.MaskingMode.Clip)
      {
        layer.Draw(canvas);
      }
      else
      {
        // Check if next layers are clipping layers
        bool hasClippingLayers = false;
        int nextIndex = i + 1;
        while (nextIndex < layers.Count && layers[nextIndex].MaskingMode == Logic.Models.MaskingMode.Clip)
        {
          if (layers[nextIndex].IsVisible) hasClippingLayers = true;
          nextIndex++;
        }

        if (hasClippingLayers)
        {
          canvas.SaveLayer();
          layer.Draw(canvas);

          using (var paint = new SKPaint { BlendMode = SKBlendMode.SrcATop })
          {
            for (int j = i + 1; j < layers.Count; j++)
            {
              var clipLayer = layers[j];
              if (clipLayer.MaskingMode != Logic.Models.MaskingMode.Clip) break;

              if (clipLayer.IsVisible)
              {
                canvas.SaveLayer(paint);
                clipLayer.Draw(canvas);
                canvas.Restore();
              }

              i = j;
            }
          }

          canvas.Restore();
        }
        else
        {
          layer.Draw(canvas);
        }
      }
    }

    viewModel.ActiveTool.DrawPreview(canvas, viewModel.CreateToolContext());

    canvas.Restore();
  }

  private void OnTouch(object? sender, SKTouchEventArgs e)
  {
    if (e.ActionType == SKTouchAction.Pressed)
    {
      CheckHideFlyouts();
    }
    viewModel?.ProcessTouch(e);
    e.Handled = true;
  }

  private void OnCanvasTapped(object? sender, TappedEventArgs e)
  {
    CheckHideFlyouts();
  }

  private void CheckHideFlyouts()
  {
    if (toolbarViewModel.IsAnyFlyoutOpen)
    {
      toolbarViewModel.IsSettingsOpen = false;
      toolbarViewModel.IsShapesFlyoutOpen = false;
      toolbarViewModel.IsBrushesFlyoutOpen = false;
    }
  }
}