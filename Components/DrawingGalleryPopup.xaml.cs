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

    // Handle collection changes to force UI refresh
    viewModel.DrawingItems.CollectionChanged += (s, e) =>
    {
      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] DrawingItems changed. Count: {viewModel.DrawingItems.Count}");

      // Force ItemsSource refresh by reassigning
      GalleryView.ItemsSource = null;
      GalleryView.ItemsSource = viewModel.DrawingItems;
    };

    System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] Initialized");

    // Subscribe to RequestClose event to handle popup dismissal
    viewModel.RequestClose += OnRequestClose;
  }

  private void OnDrawingItemTapped(object? sender, EventArgs e)
  {
    if (sender is Grid grid && grid.BindingContext is DrawingItemViewModel item)
    {
      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] Item tapped: {item.Title}");
      viewModel.OpenDrawingCommand.Execute(item).Subscribe();
    }
  }

  private void OnThumbnailImageLoaded(object? sender, EventArgs e)
  {
    if (sender is Image img)
    {
      var bindingContext = img.BindingContext as DrawingItemViewModel;
      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] Image loaded for {bindingContext?.Title ?? "unknown"}");
      System.Diagnostics.Debug.WriteLine($"  - Source: {img.Source?.GetType().Name ?? "null"}");
      System.Diagnostics.Debug.WriteLine($"  - IsVisible: {img.IsVisible}");
      System.Diagnostics.Debug.WriteLine($"  - Width: {img.Width}, Height: {img.Height}");
      System.Diagnostics.Debug.WriteLine($"  - Bounds: {img.Bounds}");
    }
  }

  private void OnThumbnailImagePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
    if (sender is Image img && e.PropertyName == "Source")
    {
      var bindingContext = img.BindingContext as DrawingItemViewModel;
      System.Diagnostics.Debug.WriteLine($"[DrawingGalleryPopup] Image.Source changed for {bindingContext?.Title ?? "unknown"}");
      System.Diagnostics.Debug.WriteLine($"  - New Source: {img.Source?.GetType().Name ?? "null"}");
      System.Diagnostics.Debug.WriteLine($"  - IsVisible: {img.IsVisible}");
    }
  }

  private async void OnRequestClose(object? sender, EventArgs e)
  {
    await this.CloseAsync();
  }

  protected override void OnHandlerChanged()
  {
    base.OnHandlerChanged();

    // Unsubscribe when handler is removed to prevent memory leaks
    if (Handler == null && BindingContext is DrawingGalleryPopupViewModel viewModel)
    {
      viewModel.RequestClose -= OnRequestClose;
    }
  }
}
