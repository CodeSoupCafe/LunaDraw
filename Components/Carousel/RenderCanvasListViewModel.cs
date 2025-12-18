using CommunityToolkit.Mvvm.Input;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Messages;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Windows.Input;

namespace LunaDraw.Components.Carousel;

public class DrawingItemState : ItemState
{
  private readonly External.Drawing drawing;

  public DrawingItemState(External.Drawing drawing)
  {
    this.drawing = drawing;
    Properties = new Dictionary<string, object?>();
  }

  public override Guid Id => drawing.Id;
  public override string Title => drawing.Name;
  public override DateTimeOffset DateCreated => drawing.LastModified;
  public override DateTimeOffset DateUpdated => drawing.LastModified;

  public External.Drawing Drawing => drawing;

  public override bool Equals(object? other)
  {
    if (other is DrawingItemState otherItem)
      return Id == otherItem.Id;
    return false;
  }

  public override int GetHashCode()
  {
    return Id.GetHashCode();
  }
}

public class RenderCanvasListViewModel : NotifyPropertyChanged
{
  private readonly IDrawingStorageMomento drawingStorage;
  private readonly IPreferencesFacade preferences;
  private readonly IMessageBus messageBus;

  private bool isGridMode;
  private bool isSortVisible;
  private bool isSearchVisible;
  private string searchText = string.Empty;
  private bool isLoading;
  private bool isRefreshing;
  private bool isEmptyCharts;
  private string chartListSortProperty = "DateUpdated";
  private string chartListSortOrder = "Descending";

  private List<DrawingItemState> allCharts = new();

  public RangedObservableCollection<DrawingItemState> Charts { get; } = new();

  public List<string> SortOrders { get; } = new() { "Ascending", "Descending" };
  public List<string> SortProperties { get; } = new() { "DateCreated", "DateUpdated", "Title" };

  public RenderCanvasListViewModel(IDrawingStorageMomento drawingStorage, IPreferencesFacade preferences, IMessageBus messageBus)
  {
    this.drawingStorage = drawingStorage;
    this.preferences = preferences;
    this.messageBus = messageBus;

    IsGridMode = this.preferences.Get<bool>(AppPreference.IsListGridView);

    ToggleViewCommand = new RelayCommand(ToggleView);
    ShowHideSortPanelCommand = new RelayCommand(ShowHideSortPanel);
    ShowHideSearchPanelCommand = new RelayCommand(ShowHideSearchPanel);
    ClearSearchPanelCommand = new RelayCommand(ClearSearchPanel);
    ReloadChartDataCommand = new AsyncRelayCommand(ReloadChartData);
    AddNewChartCommand = new AsyncRelayCommand(AddNewChart);
    OpenDrawingCommand = new Command<DrawingItemState>(OpenDrawing);
  }

  // Default constructor for XAML previewer or manual init if needed
  public RenderCanvasListViewModel() : this(new DrawingStorageMomento(), new PreferencesFacade(), new MessageBus())
  {
  }

  private IDisposable? refreshFromScroll;
  private IDisposable? refreshImagesFromScroll;
  private double lastScrollValue;
  private bool isSubscribed;
  public bool IsPaused { get; set; }

  private readonly double scrollAllowImageUpdateRange = 5;
  private readonly int scrollAllowUpdateRange = 120;
  private readonly int dropdownOutOfBoundsY = -120;

  public bool IsGridMode
  {
    get => isGridMode;
    set
    {
      if (SetProperty(ref isGridMode, value))
      {
        preferences.Set(AppPreference.IsListGridView, value);
      }
    }
  }

  public bool IsSortVisible
  {
    get => isSortVisible;
    set
    {
      if (SetProperty(ref isSortVisible, value) && value)
      {
        IsSearchVisible = false;
      }
    }
  }

  public bool IsSearchVisible
  {
    get => isSearchVisible;
    set
    {
      if (SetProperty(ref isSearchVisible, value) && value)
      {
        IsSortVisible = false;
      }
    }
  }

  public string SearchText
  {
    get => searchText;
    set
    {
      if (SetProperty(ref searchText, value))
      {
        FilterCharts();
      }
    }
  }

  public bool IsLoading
  {
    get => isLoading;
    set => SetProperty(ref isLoading, value);
  }

  public bool IsRefreshing
  {
    get => isRefreshing;
    set => SetProperty(ref isRefreshing, value);
  }

  public bool IsEmptyCharts
  {
    get => isEmptyCharts;
    set => SetProperty(ref isEmptyCharts, value);
  }

  public string ChartListSortProperty
  {
    get => chartListSortProperty;
    set
    {
      if (SetProperty(ref chartListSortProperty, value))
      {
        SortCharts();
      }
    }
  }

  public string ChartListSortOrder
  {
    get => chartListSortOrder;
    set
    {
      if (SetProperty(ref chartListSortOrder, value))
      {
        SortCharts();
      }
    }
  }

  public ICommand ToggleViewCommand { get; }
  public ICommand ShowHideSortPanelCommand { get; }
  public ICommand ShowHideSearchPanelCommand { get; }
  public ICommand ClearSearchPanelCommand { get; }
  public ICommand ReloadChartDataCommand { get; }
  public ICommand AddNewChartCommand { get; }
  public ICommand OpenDrawingCommand { get; }

  private void ToggleView()
  {
    IsGridMode = !IsGridMode;
  }

  private void ShowHideSortPanel()
  {
    IsSortVisible = !IsSortVisible;
  }

  private void ShowHideSearchPanel()
  {
    IsSearchVisible = !IsSearchVisible;
  }

  private void ClearSearchPanel()
  {
    SearchText = string.Empty;
    IsSearchVisible = false;
  }

  private void OpenDrawing(DrawingItemState drawingItem)
  {
    if (drawingItem == null) return;
    
    messageBus.SendMessage(new OpenDrawingMessage(drawingItem.Drawing));
    
    // Close the popup logic is handled by the View usually, but since we are in VM
    // we can try to find the active popup or send a message.
    // However, since we are using Toolkit Popup, we can close it from the view code-behind
    // or rely on a "RequestClose" event.
    // For simplicity, let's just trigger a Close action if we had access, but since we don't,
    // we'll let the View subscribe to the command execution or handle it in the View's event handler.
    // Actually, the View calls this command. 
    // Let's modify the View to close itself after executing this command, OR 
    // better: OpenDrawingCommand should just set the message. The View handles the closing.
  }

  private async Task ReloadChartData()
  {
    try
    {
      IsRefreshing = true;
      IsLoading = true;

      var drawings = await drawingStorage.LoadAllDrawingsAsync();
      allCharts = drawings.Select(d => new DrawingItemState(d)).ToList();

      FilterCharts();
    }
    finally
    {
      IsRefreshing = false;
      IsLoading = false;
    }
  }

  private async Task AddNewChart()
  {
    var name = await drawingStorage.GetNextDefaultNameAsync();
    var id = Guid.NewGuid();
    var newDrawing = drawingStorage.CreateExternalDrawingFromCurrent(new List<Layer>(), 800, 600, name, id);
    newDrawing.Layers.Add(new External.Layer { Id = Guid.NewGuid(), Name = "Layer 1", IsVisible = true }); //, Opacity = 255

    await drawingStorage.ExternalDrawingAsync(newDrawing);
    await ReloadChartData();
    
    // Auto-open the new drawing
    var newItem = allCharts.FirstOrDefault(c => c.Id == id);
    if (newItem != null)
    {
        OpenDrawing(newItem);
    }
  }

  private void FilterCharts()
  {
    var filtered = allCharts;

    if (!string.IsNullOrWhiteSpace(SearchText))
    {
      filtered = filtered.Where(c => c.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    SortCharts(filtered);
  }

  private void SortCharts(List<DrawingItemState>? inputList = null)
  {
    var list = inputList ?? new List<DrawingItemState>(Charts);

    Func<DrawingItemState, object> sortKey = ChartListSortProperty switch
    {
      "Title" => x => x.Title,
      "DateCreated" => x => x.DateCreated,
      _ => x => x.DateUpdated
    };

    var sorted = ChartListSortOrder == "Descending"
        ? list.OrderByDescending(sortKey).ToList()
        : list.OrderBy(sortKey).ToList();

    Charts.AddRange(sorted);

    IsEmptyCharts = Charts.Count == 0;
  }


  public void ChartGrid_Scrolled(object sender, ItemsViewScrolledEventArgs e)
  {
    refreshImagesFromScroll?.Dispose();
    refreshImagesFromScroll = null;

    refreshImagesFromScroll = new Action<Unit>((x) =>
    {
      if (lastScrollValue == e.VerticalDelta)
        _ = GlobalBroadcaster.Broadcast(new ImageLoadingState(ImageLoadingType.ForceRedraw), AppMessageStateType.ImageLoadingState);
    }).Debounce(TimeSpan.FromMilliseconds(440));

    lastScrollValue = e.VerticalDelta;

    if (e.VerticalDelta < -scrollAllowUpdateRange ||
      e.VerticalDelta > scrollAllowUpdateRange)
    {
      refreshFromScroll?.Dispose();
      refreshFromScroll = null;

      refreshFromScroll = new Action<Unit>((x) =>
      {
        if (refreshFromScroll == null)
          return;

        // MainThread.BeginInvokeOnMainThread(async () =>
        // {
        //   await HideSortPanel();
        //   await HideSearchPanel();
        // });
      }).Debounce(TimeSpan.FromMilliseconds(220));
    }
  }
}