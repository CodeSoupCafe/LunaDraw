using CommunityToolkit.Maui.Views;
using LunaDraw.Logic.ViewModels;

namespace LunaDraw.Components;

public partial class DrawingGalleryPopup : Popup
{
  private readonly DrawingGalleryPopupViewModel viewModel;

  public DrawingGalleryPopup(DrawingGalleryPopupViewModel viewModel)
  {
    this.viewModel = viewModel;
    InitializeComponent();
    BindingContext = viewModel;

    viewModel.DrawingItems.CollectionChanged += (s, e) =>
    {
      GalleryView.ItemsSource = null;
      GalleryView.ItemsSource = viewModel.DrawingItems;
    };

    viewModel.RequestClose += OnRequestClose;
  }

  private void OnDrawingItemTapped(object? sender, EventArgs e)
  {
    if (sender is Grid grid && grid.BindingContext is DrawingItemViewModel item)
    {
      viewModel.OpenDrawingCommand.Execute(item).Subscribe();
    }
  }

  private void OnThumbnailImageLoaded(object? sender, EventArgs e)
  {
  }

  private void OnThumbnailImagePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
  }

  private async void OnRequestClose(object? sender, EventArgs e)
  {
    await this.CloseAsync();
  }

  protected override void OnHandlerChanged()
  {
    base.OnHandlerChanged();

    // Unsubscribe when handler is removed to prevent memory leaks
    if (Handler == null && BindingContext is DrawingGalleryPopupViewModel vm)
    {
      vm.RequestClose -= OnRequestClose;
      
      if (vm is IDisposable disposable)
      {
          disposable.Dispose();
      }
    }
  }
}
