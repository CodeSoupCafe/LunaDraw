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

using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.ViewModels;
using LunaDraw.Logic.Extensions;
using LunaDraw.Logic.Constants;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Pages;

public partial class MainPage : ContentPage
{
  private readonly MainViewModel viewModel;
  private readonly ToolbarViewModel toolbarViewModel;
  private readonly IMessageBus messageBus;
  private readonly IPreferencesFacade preferencesFacade;
  private readonly GalleryViewModel galleryViewModel;
  private readonly IDrawingStorageMomento drawingStorageMomento;

  private MenuFlyout? canvasContextMenu;
  private MenuFlyoutSubItem? moveToLayerSubMenu;
  private bool hasShownGallery = false;
  private bool isCanvasReady = false;
  private bool isInvalidationPending = false;

  public MainPage(
      MainViewModel viewModel,
      ToolbarViewModel toolbarViewModel,
      IMessageBus messageBus,
      IPreferencesFacade preferencesFacade,
      GalleryViewModel galleryViewModel,
      IDrawingStorageMomento drawingStorageMomento)
  {
    InitializeComponent();
    this.viewModel = viewModel;
    this.toolbarViewModel = toolbarViewModel;
    this.messageBus = messageBus;
    this.preferencesFacade = preferencesFacade;
    this.galleryViewModel = galleryViewModel;
    this.drawingStorageMomento = drawingStorageMomento;

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
      SafeInvalidateSurface();
    });

    viewModel.SelectionObserver.SelectionChanged += (s, e) =>
    {
      MainThread.BeginInvokeOnMainThread(UpdateContextMenu);
    };
    viewModel.Layers.CollectionChanged += (s, e) =>
    {
      MainThread.BeginInvokeOnMainThread(UpdateContextMenu);
    };
    UpdateContextMenu();
  }

  protected override async void OnAppearing()
  {
    base.OnAppearing();

    if (!hasShownGallery)
    {
      hasShownGallery = true;
      // Yield to allow UI to render
      await Task.Delay(100);
      await ShowGalleryAsync();
    }
  }

  private async Task ShowGalleryAsync()
  {
    messageBus.SendMessage(new ShowGalleryMessage());
    await Task.CompletedTask;
  }

  private void InitializeContextMenu()
  {
    canvasContextMenu = [];

    var duplicateItem = new MenuFlyoutItem { Text = AppConstants.UI.Duplicate };
    duplicateItem.SetBinding(MenuItem.CommandProperty, new Binding("SelectionVM.DuplicateCommand", source: viewModel));
    canvasContextMenu.Add(duplicateItem);

    var copyItem = new MenuFlyoutItem { Text = AppConstants.UI.Copy };
    copyItem.SetBinding(MenuItem.CommandProperty, new Binding("SelectionVM.CopyCommand", source: viewModel));
    canvasContextMenu.Add(copyItem);

    var pasteItem = new MenuFlyoutItem { Text = AppConstants.UI.Paste };
    pasteItem.SetBinding(MenuItem.CommandProperty, new Binding("SelectionVM.PasteCommand", source: viewModel));
    canvasContextMenu.Add(pasteItem);

    canvasContextMenu.Add(new MenuFlyoutSeparator());

    var arrangeSubMenu = new MenuFlyoutSubItem { Text = AppConstants.UI.Arrange };

    var sendToBackItem = new MenuFlyoutItem { Text = AppConstants.UI.SendToBack };
    sendToBackItem.SetBinding(MenuItem.CommandProperty, new Binding("SelectionVM.SendElementToBackCommand", source: viewModel));
    arrangeSubMenu.Add(sendToBackItem);

    var sendBackwardItem = new MenuFlyoutItem { Text = AppConstants.UI.SendBackward };
    sendBackwardItem.SetBinding(MenuItem.CommandProperty, new Binding("SelectionVM.SendBackwardCommand", source: viewModel));
    arrangeSubMenu.Add(sendBackwardItem);

    var bringForwardItem = new MenuFlyoutItem { Text = AppConstants.UI.BringForward };
    bringForwardItem.SetBinding(MenuItem.CommandProperty, new Binding("SelectionVM.BringForwardCommand", source: viewModel));
    arrangeSubMenu.Add(bringForwardItem);

    var sendToFrontItem = new MenuFlyoutItem { Text = AppConstants.UI.SendToFront };
    sendToFrontItem.SetBinding(MenuItem.CommandProperty, new Binding("SelectionVM.BringElementToFrontCommand", source: viewModel));
    arrangeSubMenu.Add(sendToFrontItem);

    canvasContextMenu.Add(arrangeSubMenu);
    canvasContextMenu.Add(new MenuFlyoutSeparator());

    moveToLayerSubMenu = new MenuFlyoutSubItem { Text = AppConstants.UI.MoveTo };

    canvasContextMenu.Add(moveToLayerSubMenu);

    FlyoutBase.SetContextFlyout(canvasView, canvasContextMenu);
  }

  private void UpdateContextMenu()
  {
    if (moveToLayerSubMenu == null) return;

    try
    {
      moveToLayerSubMenu.Clear();

      var addLayerItem = new MenuFlyoutItem { Text = AppConstants.UI.NewLayer };
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
    catch (Exception)
    {
    }
  }

  private void SafeInvalidateSurface()
  {
    if (canvasView == null)
    {
      return;
    }

    if (!isCanvasReady)
    {
      isInvalidationPending = true;
      return;
    }

    // Ensure we're on the main thread and add error handling
    MainThread.BeginInvokeOnMainThread(() =>
    {
      try
      {
        if (canvasView != null)
        {
          canvasView.InvalidateSurface();
        }
      }
      catch (Exception ex)
      {
        if (ex.Message.Contains("EGL"))
        {
          isCanvasReady = false;
          isInvalidationPending = true;

          // Retry after a delay
          Task.Delay(100).ContinueWith(_ =>
          {
            SafeInvalidateSurface();
          });
        }
      }
    });
  }

  private void OnCanvasViewPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
  {
    try
    {
      if (!isCanvasReady)
      {
        isCanvasReady = true;

        if (isInvalidationPending)
        {
          isInvalidationPending = false;
          Task.Delay(50).ContinueWith(_ => SafeInvalidateSurface());
        }
      }

      SKSurface surface = e.Surface;
      SKCanvas canvas = surface.Canvas;

      int width = e.BackendRenderTarget.Width;
      int height = e.BackendRenderTarget.Height;

      if (viewModel is null) return;

      viewModel.CanvasSize = new SKRect(0, 0, width, height);
      viewModel.NavigationModel.CanvasWidth = width;
      viewModel.NavigationModel.CanvasHeight = height;

      var bgColor = preferencesFacade.GetCanvasBackgroundColor();
      canvas.Clear(bgColor);

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

            using (var paint = new SKPaint { BlendMode = SKBlendMode.SrcATop, IsAntialias = true })
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
    catch (Exception)
    {
    }
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