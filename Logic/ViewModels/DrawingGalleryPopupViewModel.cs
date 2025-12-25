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

using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Drawing;
using LunaDraw.Logic.Messages;
using CodeSoupCafe.Maui.Infrastructure;
using CodeSoupCafe.Maui.Models;

namespace LunaDraw.Logic.ViewModels;

public class DrawingGalleryPopupViewModel : ReactiveObject, IDisposable
{
  private readonly GalleryViewModel galleryViewModel;
  private readonly IDrawingThumbnailHandler thumbnailService;
  private readonly IMessageBus messageBus;
  private IDisposable? drawingListChangedSubscription;

  private RangedObservableCollection<DrawingItemViewModel> drawingItems = [];
  private bool isLoading;

  public RangedObservableCollection<DrawingItemViewModel> DrawingItems
  {
    get => drawingItems;
    set => this.RaiseAndSetIfChanged(ref drawingItems, value);
  }

  public bool IsLoading
  {
    get => isLoading;
    set => this.RaiseAndSetIfChanged(ref isLoading, value);
  }

  public ReactiveCommand<Unit, Unit> NewDrawingCommand { get; }
  public ReactiveCommand<Unit, Unit> CancelCommand { get; }
  public ReactiveCommand<DrawingItemViewModel, Unit> OpenDrawingCommand { get; }
  public ReactiveCommand<Unit, Unit> LoadDrawingsCommand { get; }
  public ReactiveCommand<DrawingItemViewModel, Unit> DuplicateDrawingCommand { get; }
  public ReactiveCommand<DrawingItemViewModel, Unit> DeleteDrawingCommand { get; }
  public ReactiveCommand<DrawingItemViewModel, Unit> RenameDrawingCommand { get; }

  private List<GalleryContextCommand>? contextCommands;
  public List<GalleryContextCommand>? ContextCommands
  {
    get => contextCommands;
    set => this.RaiseAndSetIfChanged(ref contextCommands, value);
  }


  public DrawingGalleryPopupViewModel(
    GalleryViewModel galleryViewModel,
    IDrawingThumbnailHandler thumbnailService,
    IMessageBus messageBus)
  {
    this.galleryViewModel = galleryViewModel;
    this.thumbnailService = thumbnailService;
    this.messageBus = messageBus;

    NewDrawingCommand = ReactiveCommand.Create(() =>
    {
      galleryViewModel.NewDrawingCommand.Execute().Subscribe();
      RequestClose?.Invoke(this, EventArgs.Empty);
    });

    CancelCommand = ReactiveCommand.Create(() =>
    {
      RequestClose?.Invoke(this, EventArgs.Empty);
    });

    OpenDrawingCommand = ReactiveCommand.Create<DrawingItemViewModel>(item =>
    {
      if (item?.Drawing != null)
      {
        messageBus.SendMessage(new OpenDrawingMessage(item.Drawing));
        RequestClose?.Invoke(this, EventArgs.Empty);
      }
    });

    LoadDrawingsCommand = ReactiveCommand.CreateFromTask(LoadDrawingsAsync);
    LoadDrawingsCommand.Execute().Subscribe();


    DuplicateDrawingCommand = ReactiveCommand.CreateFromTask<DrawingItemViewModel>(DuplicateDrawingAsync);
    DeleteDrawingCommand = ReactiveCommand.CreateFromTask<DrawingItemViewModel>(DeleteDrawingAsync);
    RenameDrawingCommand = ReactiveCommand.CreateFromTask<DrawingItemViewModel>(RenameDrawingAsync);

    ContextCommands = new List<GalleryContextCommand>
    {
      new("Duplicate", DuplicateDrawingCommand),
      new("Rename", RenameDrawingCommand),
      new("Delete", DeleteDrawingCommand, isDestructive: true)
    };
    drawingListChangedSubscription = messageBus.Listen<DrawingListChangedMessage>()
      .ObserveOn(RxApp.MainThreadScheduler)
      .Subscribe(async msg =>
      {
        System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] DrawingListChangedMessage received. DrawingId: {msg.DrawingId}");

        if (msg.DrawingId.HasValue)
        {
          var drawingId = msg.DrawingId.Value;
          var existingItem = DrawingItems.FirstOrDefault(x => x.Drawing.Id == drawingId);

          // Reload metadata to get latest info
          var updatedDrawing = await galleryViewModel.ReloadDrawingMetadataAsync(drawingId);

          if (updatedDrawing == null)
          {
            // Drawing might have been deleted?
            if (existingItem != null)
            {
              DrawingItems.Remove(existingItem);
            }
            return;
          }

          // Invalidate thumbnail cache immediately
          // Invalidate the thumbnail - it will reload lazily when item appears
          await thumbnailService.InvalidateThumbnailAsync(msg.DrawingId.Value);
          // existingItem?.ThumbnailBase64 = null;

          if (existingItem != null)
          {
            System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] Updating existing item: {drawingId}");

            // Update metadata
            existingItem.UpdateDrawingMetadata(updatedDrawing);
            existingItem.ThumbnailBase64 = null; // Force reload on next appear

            // Re-sort logic
            var index = DrawingItems.IndexOf(existingItem);
            if (index >= 0)
            {
              DrawingItems.RemoveAt(index);

              var insertIndex = 0;
              for (int i = 0; i < DrawingItems.Count; i++)
              {
                if (existingItem.DateUpdated > DrawingItems[i].DateUpdated)
                {
                  insertIndex = i;
                  break;
                }
                insertIndex = i + 1;
              }
              DrawingItems.Insert(insertIndex, existingItem);

              // If the item is currently visible, we might want to trigger a thumbnail reload
              // However, since we set ThumbnailBase64 to null, the bindings should update.
              // If the item remains in view, the control might not fire OnAppearing again.
              // We can manually trigger a reload here if needed.
              _ = existingItem.LoadThumbnailAsync(thumbnailService);
            }
          }
          else
          {
            System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] Adding new item: {drawingId}");

            // New drawing created externally
            var newItem = new DrawingItemViewModel(updatedDrawing);
            DrawingItems.Insert(0, newItem); // Assume newest

            // Trigger load
            _ = newItem.LoadThumbnailAsync(thumbnailService);
          }
        }
        else
        {
          // Full reload requested
          System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] Full reload requested via message");
          await LoadDrawingsAsync();
        }
      });
  }

  public event EventHandler? RequestClose;

  public Action<ISortable> OnItemAppearing => HandleItemAppearing;
  public Action<ISortable> OnItemDisappearing => HandleItemDisappearing;

  private void HandleItemAppearing(ISortable item)
  {
    if (item is DrawingItemViewModel viewModel)
    {
      // Fire and forget - load thumbnail async
      _ = viewModel.LoadThumbnailAsync(thumbnailService);
    }
  }

  private void HandleItemDisappearing(ISortable item)
  {
    // Optional: unload thumbnail to save memory for very large galleries
    // For now, keep thumbnails loaded once fetched (they're cached)
  }

  private async Task LoadDrawingsAsync()
  {
    IsLoading = true;

    try
    {
      System.Diagnostics.Debug.WriteLine("[DrawingGalleryPopup] Loading drawings...");
      await galleryViewModel.LoadDrawingsCommand.Execute().GetAwaiter();

      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] GalleryViewModel.Drawings count: {galleryViewModel.Drawings.Count}");

      DrawingItems.ClearAndStaySilent();

      // Load metadata only - NO thumbnail generation
      var items = galleryViewModel.Drawings
        .Select(drawing => new DrawingItemViewModel(drawing))
        .ToList();

      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] Created {items.Count} DrawingItemViewModels");

      DrawingItems.AddRange(items);

      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] DrawingItems.Count after AddRange: {DrawingItems.Count}");

      // Force property change notification to trigger ItemGalleryView binding
      this.RaisePropertyChanged(nameof(DrawingItems));
      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] Raised property changed for DrawingItems");
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] ERROR: {ex.Message}");
      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] Stack: {ex.StackTrace}");
    }
    finally
    {
      IsLoading = false;
    }
  }
  private async Task DuplicateDrawingAsync(DrawingItemViewModel item)
  {
    if (item?.Drawing == null) return;
    await galleryViewModel.DuplicateDrawingCommand.Execute(item.Drawing).GetAwaiter();
  }

  private async Task DeleteDrawingAsync(DrawingItemViewModel item)
  {
    if (item?.Drawing == null) return;

    var page = Application.Current?.Windows[0]?.Page;
    if (page != null)
    {
      var confirmed = await page.DisplayAlertAsync(
        "Delete Drawing",
        $"Are you sure you want to delete '{item.Title}'?",
        "Delete",
        "Cancel");

      if (!confirmed) return;
      await galleryViewModel.DeleteDrawingCommand.Execute(item.Drawing).GetAwaiter();
    }
  }

  private async Task RenameDrawingAsync(DrawingItemViewModel item)
  {
    if (item?.Drawing == null) return;

    var page = Application.Current?.Windows[0]?.Page;
    if (page != null)
    {
      var newName = await page.DisplayPromptAsync(
          "Rename Drawing",
          "Enter new name:",
          initialValue: item.Title,
          maxLength: 50,
          placeholder: "Drawing name") ?? string.Empty;

      if (string.IsNullOrWhiteSpace(newName)) return;

      await galleryViewModel.RenameDrawing(item.Drawing, newName);
      messageBus.SendMessage(new DrawingListChangedMessage(item.Drawing.Id));
    }
  }

  public void Dispose()
  {
    drawingListChangedSubscription?.Dispose();
    drawingListChangedSubscription = null;
  }
}

/// <summary>
/// Wrapper class that provides lazy thumbnail loading for External.Drawing.
/// Inherits from ItemState for compatibility with CodeSoupCafe.Maui.Carousel library.
/// </summary>
public class DrawingItemViewModel : ItemState, INotifyPropertyChanged
{
  private readonly External.Drawing drawing;
  private string? thumbnailBase64;
  private bool isLoadingThumbnail;

  public event PropertyChangedEventHandler? PropertyChanged;

  public DrawingItemViewModel(External.Drawing drawing)
  {
    this.drawing = drawing;
  }

  public External.Drawing Drawing => drawing;

  public override string? ThumbnailBase64
  {
    get => thumbnailBase64;
    set
    {
      if (thumbnailBase64 != value)
      {
        thumbnailBase64 = value;
        OnPropertyChanged();
      }
    }
  }

  public bool IsLoadingThumbnail
  {
    get => isLoadingThumbnail;
    set
    {
      if (isLoadingThumbnail != value)
      {
        isLoadingThumbnail = value;
        OnPropertyChanged();
      }
    }
  }

  protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  // ItemState abstract property implementations
  public override Guid Id => drawing.Id;
  public override string Title => drawing.Title;
  public override DateTimeOffset DateCreated => drawing.DateCreated;
  public override DateTimeOffset DateUpdated => drawing.DateUpdated;

  /// <summary>
  /// Loads the thumbnail asynchronously when the item appears in view.
  /// Called by the lifecycle callback from the gallery control.
  /// </summary>
  public async Task LoadThumbnailAsync(IDrawingThumbnailHandler thumbnailService)
  {
    if (ThumbnailBase64 != null || IsLoadingThumbnail)
    {
      return; // Already loaded or loading
    }

    IsLoadingThumbnail = true;
    try
    {
      ThumbnailBase64 = await thumbnailService.GetThumbnailBase64Async(
        Drawing.Id,
        width: 300,
        height: 300,
        drawing: Drawing);
    }
    finally
    {
      IsLoadingThumbnail = false;
    }
  }

  /// <summary>
  /// Updates the underlying drawing metadata and raises property change notifications
  /// to trigger re-sorting in the gallery control.
  /// </summary>
  public void UpdateDrawingMetadata(External.Drawing updatedDrawing)
  {
    if (updatedDrawing.Id != drawing.Id)
    {
      return; // Safety check - don't update with wrong drawing
    }

    // Update properties of the existing drawing object
    drawing.Name = updatedDrawing.Name;
    drawing.LastModified = updatedDrawing.LastModified;
    drawing.CanvasWidth = updatedDrawing.CanvasWidth;
    drawing.CanvasHeight = updatedDrawing.CanvasHeight;

    // Raise property change notifications for ItemState properties
    // This triggers the CodeSoupCafe.Maui control to re-sort
    OnPropertyChanged(nameof(Title));
    OnPropertyChanged(nameof(DateUpdated));
    OnPropertyChanged(nameof(DateCreated));
  }

  public override bool Equals(object? obj) =>
    obj is DrawingItemViewModel other && Id == other.Id;

  public override int GetHashCode() => Id.GetHashCode();
}
