using CommunityToolkit.Maui.Views;
using LunaDraw.Logic.ViewModels;
using LunaDraw.Logic.BurnCalcPort.Components;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices; // Added
using Microsoft.Maui.Graphics; // Added

namespace LunaDraw.Components;

public partial class GalleryPopup : CommunityToolkit.Maui.Views.Popup, IGalleryListView
{
  private readonly GalleryViewModel viewModel;
  private readonly RenderListViewAdapter listAdapter;

  public GalleryPopup(GalleryViewModel viewModel)
  {
    InitializeComponent();
    this.viewModel = viewModel;
    BindingContext = viewModel;

    listAdapter = new GalleryListViewAdapter();

    var displayInfo = DeviceDisplay.MainDisplayInfo;
    var width = displayInfo.Width / displayInfo.Density;
    var height = displayInfo.Height / displayInfo.Density;

    if (Content != null)
    {
        Content.WidthRequest = width;
        Content.HeightRequest = height;
    }

    viewModel.NewDrawingCommand.Subscribe(_ => 
    {
        viewModel.IsNewDrawingRequested = true;
        viewModel.SelectedDrawing = null;
        MainThread.BeginInvokeOnMainThread(async () => await this.CloseAsync());
    });
    
    viewModel.OpenDrawingCommand.Subscribe(drawing => 
    {
        viewModel.IsNewDrawingRequested = false;
        viewModel.SelectedDrawing = drawing;
    });

    Opened += OnOpened;
  }

  // IGalleryListView Implementation
  public CollectionView CollectionView => GalleryCollectionView;
  public GridItemsLayout GridLayout => GalleryGridLayout;
  public GalleryViewModel ViewModel => viewModel;

  private void OnOpened(object? sender, EventArgs e)
  {
      listAdapter.OnAppearing(this);
  }

  private void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
  {
      listAdapter.ChartGrid_Scrolled(sender, e);
  }
}