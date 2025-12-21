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
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Messages;
using CodeSoupCafe.Maui.Infrastructure;

namespace LunaDraw.Logic.ViewModels;

public class DrawingGalleryPopupViewModel : ReactiveObject
{
  private readonly GalleryViewModel galleryViewModel;
  private readonly IDrawingThumbnailFacade thumbnailService;
  private readonly IMessageBus messageBus;

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

  public DrawingGalleryPopupViewModel(
    GalleryViewModel galleryViewModel,
    IDrawingThumbnailFacade thumbnailService,
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

    messageBus.Listen<DrawingListChangedMessage>()
      .Subscribe(async msg =>
      {
        if (msg.DrawingId.HasValue)
        {
          var existingItem = DrawingItems.FirstOrDefault(x => x.Drawing.Id == msg.DrawingId.Value);
          if (existingItem != null)
          {
            // Reload the drawing metadata from storage to get the updated LastModified/DateUpdated
            var updatedDrawing = await galleryViewModel.ReloadDrawingMetadataAsync(msg.DrawingId.Value);
            if (updatedDrawing != null)
            {
              // Update the metadata which triggers re-sorting via ISortable property change notifications
              existingItem.UpdateDrawingMetadata(updatedDrawing);
            }

            // Update the thumbnail
            thumbnailService.InvalidateThumbnail(msg.DrawingId.Value);
            var newThumbnail = await thumbnailService.GetThumbnailAsync(msg.DrawingId.Value, 300, 300);
            existingItem.ThumbnailSource = newThumbnail;

            // Trigger re-sort by removing and re-adding the item
            // This ensures the gallery control picks up the new DateUpdated and re-sorts
            var index = DrawingItems.IndexOf(existingItem);
            if (index >= 0)
            {
              DrawingItems.RemoveAt(index);

              // Find the correct position based on DateUpdated (descending order)
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
            }
          }
        }
      });
  }

  public event EventHandler? RequestClose;

  private async Task LoadDrawingsAsync()
  {
    IsLoading = true;

    try
    {
      await galleryViewModel.LoadDrawingsCommand.Execute().GetAwaiter();

      DrawingItems.ClearAndStaySilent();

      var items = new List<DrawingItemViewModel>();

      foreach (var drawing in galleryViewModel.Drawings)
      {
        var thumbnailSource = await thumbnailService.GetThumbnailAsync(
          drawing.Id,
          width: 300,
          height: 300);

        var item = new DrawingItemViewModel(drawing, thumbnailSource);
        items.Add(item);
      }

      DrawingItems.AddRange(items);
    }
    finally
    {
      IsLoading = false;
    }
  }
}

/// <summary>
/// Wrapper class that adds ThumbnailSource to External.Drawing without modifying the domain model.
/// Implements ISortable for compatibility with CodeSoupCafe.Maui.Carousel library.
/// </summary>
public class DrawingItemViewModel : ReactiveObject, CodeSoupCafe.Maui.Models.ISortable
{
  private readonly External.Drawing drawing;
  private ImageSource? thumbnailSource;

  public DrawingItemViewModel(External.Drawing drawing, ImageSource? thumbnail)
  {
    this.drawing = drawing;
    this.thumbnailSource = thumbnail;
  }

  public External.Drawing Drawing => drawing;

  public ImageSource? ThumbnailSource
  {
    get => thumbnailSource;
    set => this.RaiseAndSetIfChanged(ref thumbnailSource, value);
  }

  // ISortable implementation (delegate to Drawing)
  public Guid Id => drawing.Id;
  public string Title => drawing.Title;
  public DateTimeOffset DateCreated => drawing.DateCreated;
  public DateTimeOffset DateUpdated => drawing.DateUpdated;

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

    // Raise property change notifications for ISortable properties
    // This triggers the CodeSoupCafe.Maui control to re-sort
    this.RaisePropertyChanged(nameof(Title));
    this.RaisePropertyChanged(nameof(DateUpdated));
    this.RaisePropertyChanged(nameof(DateCreated));
  }

  public override bool Equals(object? obj) =>
    obj is DrawingItemViewModel other && Id == other.Id;

  public override int GetHashCode() => Id.GetHashCode();
}
