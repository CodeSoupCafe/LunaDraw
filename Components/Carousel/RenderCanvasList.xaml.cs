using CommunityToolkit.Maui.Views;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Utils;
using Microsoft.Maui.Controls;
using ReactiveUI;

namespace LunaDraw.Components.Carousel;

public partial class RenderCanvasList : CommunityToolkit.Maui.Views.Popup
{
  public RenderCanvasListViewModel ViewModel { get; }

  private void ChartGrid_Scrolled(object sender, ItemsViewScrolledEventArgs e) => ViewModel?.ChartGrid_Scrolled(sender, e);

  public RenderCanvasList(IDrawingStorageMomento drawingStorage, IPreferencesFacade preferences, IMessageBus messageBus)
  {
    InitializeComponent();

    ViewModel = new RenderCanvasListViewModel(drawingStorage, preferences, messageBus);
    BindingContext = this;

    Opened += OnOpened;
    Closed += OnClosed;
    ViewModel.RequestClose += async (s, e) =>
    {
        try
        {
            await CloseAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error closing popup: {ex}");
        }
    };
  }

  private void OnClosed(object? sender, EventArgs e)
  {
    ViewModel?.Dispose();
  }

  private void OnOpened(object? sender, EventArgs e)
  {
      ViewModel.ReloadChartDataCommand.Execute(null);
  }

  public async void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
      if (e.CurrentSelection.FirstOrDefault() is DrawingItemState item)
      {
          System.Diagnostics.Debug.WriteLine($"[RenderCanvasList] Item selected: {item.Title}");

          // Clear selection immediately to prevent double-clicks
          if (sender is CollectionView collectionView)
          {
              collectionView.SelectedItem = null;
          }

          // Execute the async command
          try
          {
              await ViewModel.OpenDrawingCommand.ExecuteAsync(item);
          }
          catch (Exception ex)
          {
              System.Diagnostics.Debug.WriteLine($"[RenderCanvasList] Error executing OpenDrawingCommand: {ex}");
          }
      }
  }
}
