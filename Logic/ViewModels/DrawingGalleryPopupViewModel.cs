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

  private RangedObservableCollection<DrawingItemViewModel> drawingItems = new();
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

    // Auto-load drawings when ViewModel is created
    LoadDrawingsCommand.Execute().Subscribe();
  }

  public event EventHandler? RequestClose;

  private async Task LoadDrawingsAsync()
  {
    IsLoading = true;
    System.Diagnostics.Debug.WriteLine("[DrawingGalleryPopupViewModel] LoadDrawingsAsync started");

    try
    {
      // Load drawings from GalleryViewModel
      await galleryViewModel.LoadDrawingsCommand.Execute().GetAwaiter();
      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopupViewModel] GalleryViewModel has {galleryViewModel.Drawings.Count} drawings");

      // Clear existing items using RangedObservableCollection for performance
      DrawingItems.ClearAndStaySilent();

      // Create wrapped items with thumbnails
      var items = new List<DrawingItemViewModel>();

      foreach (var drawing in galleryViewModel.Drawings)
      {
        System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopupViewModel] Processing drawing: {drawing.Title} (ID: {drawing.Id})");

        // Generate thumbnail asynchronously
        var thumbnailSource = await thumbnailService.GetThumbnailAsync(
          drawing.Id,
          width: 300,
          height: 300);

        if (thumbnailSource == null)
        {
          System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopupViewModel] WARNING: Thumbnail is NULL for {drawing.Title}");
        }
        else
        {
          System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopupViewModel] Thumbnail loaded for {drawing.Title}");
        }

        var item = new DrawingItemViewModel(drawing, thumbnailSource);
        items.Add(item);
      }

      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopupViewModel] Created {items.Count} DrawingItemViewModels with thumbnails");

      // Add all items at once using RangedObservableCollection for performance
      DrawingItems.AddRange(items);

      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopupViewModel] DrawingItems.Count after AddRange: {DrawingItems.Count}");
    }
    finally
    {
      IsLoading = false;
      System.Diagnostics.Debug.WriteLine("[DrawingGalleryPopupViewModel] LoadDrawingsAsync finished");
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

  public override bool Equals(object? obj) =>
    obj is DrawingItemViewModel other && Id == other.Id;

  public override int GetHashCode() => Id.GetHashCode();
}
