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

    // Initialize Context Menu in Code Behind
    InitializeContextMenu();

    // Set up flyout content binding contexts
    SettingsFlyoutContent.BindingContext = this.toolbarViewModel;
    ShapesFlyoutContent.BindingContext = this.toolbarViewModel;
    BrushesFlyoutContent.BindingContext = this.toolbarViewModel;

    canvasView.PaintSurface += OnCanvasViewPaintSurface;
    canvasView.Touch += OnTouch;

    this.messageBus.Listen<CanvasInvalidateMessage>().Subscribe(_ =>
    {
      canvasView?.InvalidateSurface();
    });

    // Reactive Context Menu Updates
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

      moveToLayerSubMenu = new MenuFlyoutSubItem { Text = "Move to",  };

      canvasContextMenu.Add(moveToLayerSubMenu);

      // Assign to View using Attached Property
      FlyoutBase.SetContextFlyout(canvasView, canvasContextMenu);
  }

  private void UpdateContextMenu()
  {
      if (moveToLayerSubMenu == null) return;

      // Populate "Move to Layer" submenu dynamically
      moveToLayerSubMenu.Clear();

      var addLayerItem = new MenuFlyoutItem { Text = "New Layer" };
      addLayerItem.SetBinding(MenuItem.CommandProperty, new Binding("LayerPanelVM.AddLayerCommand", source: viewModel));
      moveToLayerSubMenu.Add(addLayerItem);

      bool hasSelection = viewModel.SelectedElements.Any();
      moveToLayerSubMenu.IsEnabled = hasSelection;
      
      // Enable/Disable Send/Bring commands based on selection?
      // Commands usually handle CanExecute, but MenuFlyoutItem might not update automatically on Mac/Windows sometimes?
      // ReactiveUI commands handle CanExecute.

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

    // Ensure ViewModel knows the current canvas size (logical pixels)
    int width = e.BackendRenderTarget.Width;
    int height = e.BackendRenderTarget.Height;
    viewModel.CanvasSize = new SKRect(0, 0, width, height);

    canvas.Clear(SKColors.White);

    if (viewModel == null) return;

    canvas.Save();

    // DIRECT FIX: Use SetMatrix directly with UserMatrix.
    // The UserMatrix is now the single source of truth for View Transformation (Pan/Zoom/Rotate).
    // We do not mix it with MaxScaleCentered or other legacy logic.
    canvas.SetMatrix(viewModel.NavigationModel.UserMatrix);
    
    // Sync TotalMatrix for Input Handler (reverse mapping)
    // Since we just SetMatrix, TotalMatrix IS UserMatrix.
    viewModel.NavigationModel.TotalMatrix = canvas.TotalMatrix;

    // Drawing Loop with Masking Support
    // We group layers into "Clipping Groups".
    // A group starts with a Base Layer (MaskingMode != Clip).
    // Subsequent layers with MaskingMode == Clip belong to this group and are clipped to the Base Layer.
    
    var layers = viewModel.Layers;
    for (int i = 0; i < layers.Count; i++)
    {
        var layer = layers[i];
        if (!layer.IsVisible) continue;

        if (layer.MaskingMode == Logic.Models.MaskingMode.Clip)
        {
            // Clip layers should have been handled by the previous Base layer loop.
            // If we encounter a Clip layer here, it means it has no Base layer (it's the first layer or follows invisible layers?)
            // In that case, just draw it normally (fallback).
             layer.Draw(canvas);
        }
        else
        {
            // This is a Base Layer.
            // Check if next layers are Clip layers.
            bool hasClippingLayers = false;
            int nextIndex = i + 1;
            while(nextIndex < layers.Count && layers[nextIndex].MaskingMode == Logic.Models.MaskingMode.Clip)
            {
                if (layers[nextIndex].IsVisible) hasClippingLayers = true;
                nextIndex++;
            }

            if (hasClippingLayers)
            {
                // Start a Transparency Layer for the Group
                // This isolates the blending of this group from the background
                canvas.SaveLayer();

                // Draw Base Layer
                layer.Draw(canvas);

                // Draw Clipping Layers
                // They use SrcATop to replace the Base Layer pixels ONLY where Base Layer is opaque.
                using (var paint = new SKPaint { BlendMode = SKBlendMode.SrcATop })
                {
                    for (int j = i + 1; j < layers.Count; j++)
                    {
                        var clipLayer = layers[j];
                        if (clipLayer.MaskingMode != Logic.Models.MaskingMode.Clip) break; // End of group
                        
                        if (clipLayer.IsVisible)
                        {
                            canvas.SaveLayer(paint);
                            clipLayer.Draw(canvas);
                            canvas.Restore();
                        }
                        
                        // Advance outer loop index
                        i = j;
                    }
                }

                canvas.Restore();
            }
            else
            {
                // Simple draw
                layer.Draw(canvas);
            }
        }
    }

    // UPDATED: Create tool context for preview
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
