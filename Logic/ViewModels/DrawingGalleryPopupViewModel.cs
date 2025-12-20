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
            thumbnailService.InvalidateThumbnail(msg.DrawingId.Value);
            var newThumbnail = await thumbnailService.GetThumbnailAsync(msg.DrawingId.Value, 300, 300);
            existingItem.ThumbnailSource = newThumbnail;
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

  public override bool Equals(object? obj) =>
    obj is DrawingItemViewModel other && Id == other.Id;

  public override int GetHashCode() => Id.GetHashCode();
}
