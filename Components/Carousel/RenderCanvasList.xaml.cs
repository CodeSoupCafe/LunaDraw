using LunaDraw.Logic.Models;
using LunaDraw.Logic.Utils;
using Microsoft.Maui.Controls;
using ReactiveUI;

namespace LunaDraw.Components.Carousel;

public partial class RenderCanvasList : ContentPage
{
  public RenderCanvasListViewModel ViewModel { get; }

  public RenderCanvasList()
  {
    InitializeComponent();

    // Manual resolution if DI not working directly for this page or strictly adhering to "new component" pattern without touching MauiProgram.cs
    // Assuming Services are available via Handler or fallback to default
    var services = Application.Current?.Handler?.MauiContext?.Services;
    var drawingStorage = services?.GetService<IDrawingStorageMomento>() ?? new DrawingStorageMomento();
    var preferences = services?.GetService<IPreferencesFacade>() ?? new PreferencesFacade();

    ViewModel = new RenderCanvasListViewModel(drawingStorage, preferences);
    BindingContext = ViewModel;
  }

  protected override async void OnAppearing()
  {
    base.OnAppearing();
    ViewModel.ReloadChartDataCommand.Execute(null);
  }

  private void ListView_ItemTapped(object sender, ItemTappedEventArgs e)
  {
    if (e.Item is DrawingItemState item)
    {
      // Handle selection
      // If this page is used in a Popup or Navigation stack, we might want to close or notify.
      // For now, we'll try to use the MessageBus if available or just expose an event.
      // But to "work" in the context of LunaDraw, we probably want to load this drawing.

      // Assuming we want to notify MainViewModel or similar.
      // Since we can't easily touch MainViewModel, we'll assume the caller observes this or we use MessageBus.

      var services = Application.Current?.Handler?.MauiContext?.Services;
      var messageBus = services?.GetService<IMessageBus>();

      // We could send a message if one existed for "LoadDrawing".
      // Logic/Messages/DrawingStateChangedMessage.cs might be relevant.

      // For now, simple console log or check if we are in a Navigation context.
      System.Diagnostics.Debug.WriteLine($"Selected Drawing: {item.Title} ({item.Id})");
    }

    // Deselect item
    if (sender is ListView list)
      list.SelectedItem = null;
  }

  private void ChartGrid_Scrolled(object sender, ItemsViewScrolledEventArgs e)
  {
    // Scroll handling for lazy loading or UI effects
    // Currently handled by ViewModel loading all data, so this can be empty or used for infinite scroll later.
  }
}
