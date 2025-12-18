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
  }

  private void OnOpened(object? sender, EventArgs e)
  {
      ViewModel.ReloadChartDataCommand.Execute(null);
  }

  public async void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
      if (e.CurrentSelection.FirstOrDefault() is DrawingItemState item)
      {
          ViewModel.OpenDrawingCommand.Execute(item);
          await CloseAsync(); // Close the popup
      }
      
      if (sender is CollectionView collectionView)
      {
          collectionView.SelectedItem = null;
      }
  }
}
