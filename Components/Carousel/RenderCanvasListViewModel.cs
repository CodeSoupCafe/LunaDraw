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
  }

  public override Guid Id => drawing.Id;
  public override string Title => drawing.Name;
  public override DateTimeOffset DateCreated => drawing.LastModified;
  public override DateTimeOffset DateUpdated => drawing.LastModified;

  public External.Drawing Drawing => drawing;

  public override bool Equals(object? other)
  {
    return other is DrawingItemState otherItem && Id == otherItem.Id;
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
  private IDisposable? drawingListChangedSubscription;

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
    OpenDrawingCommand = new AsyncRelayCommand<DrawingItemState>(OpenDrawingAsync);

    // Subscribe to drawing list changes to refresh gallery
    drawingListChangedSubscription = this.messageBus.Listen<LunaDraw.Logic.Messages.DrawingListChangedMessage>()
      .Subscribe(_ => MainThread.BeginInvokeOnMainThread(async () => await ReloadChartData()));
  }

  public void Dispose()
  {
    drawingListChangedSubscription?.Dispose();
  }

  // Default constructor for XAML previewer or manual init if needed
  public RenderCanvasListViewModel() : this(new DrawingStorageMomento(), new PreferencesFacade(), new MessageBus())
  {
  }

  private IDisposable? refreshFromScroll;
  private IDisposable? refreshImagesFromScroll;
  private double lastScrollValue;
  private readonly int scrollAllowUpdateRange = 120;

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
  public IAsyncRelayCommand<DrawingItemState> OpenDrawingCommand { get; }
  
  public event EventHandler? RequestClose;

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

  private async Task OpenDrawingAsync(DrawingItemState? drawingItem)
  {
    if (drawingItem == null)
    {
      System.Diagnostics.Debug.WriteLine("[RenderCanvasListViewModel] OpenDrawing called with null item");
      return;
    }

    try
    {
      System.Diagnostics.Debug.WriteLine($"[RenderCanvasListViewModel] Opening drawing: {drawingItem.Title} ({drawingItem.Id})");

      // Store the drawing reference
      var drawing = drawingItem.Drawing;

      // Send the message to load the drawing FIRST
      System.Diagnostics.Debug.WriteLine($"[RenderCanvasListViewModel] Sending OpenDrawingMessage");
      messageBus.SendMessage(new OpenDrawingMessage(drawing));

      // Small delay to let the message get processed
      await Task.Delay(50);

      // Then close the popup
      System.Diagnostics.Debug.WriteLine($"[RenderCanvasListViewModel] Closing popup");
      RequestClose?.Invoke(this, EventArgs.Empty);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"[RenderCanvasListViewModel] Error opening drawing: {ex}");
      System.Diagnostics.Debug.WriteLine($"[RenderCanvasListViewModel] Stack trace: {ex.StackTrace}");
    }
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
    newDrawing.Layers.Add(new External.Layer { Id = Guid.NewGuid(), Name = "Layer 1", IsVisible = true });

    await drawingStorage.ExternalDrawingAsync(newDrawing);

    // Notify that the drawing list changed (will trigger ReloadChartData via subscription)
    messageBus.SendMessage(new LunaDraw.Logic.Messages.DrawingListChangedMessage());

    // Wait a moment for the reload to complete
    await Task.Delay(100);

    // Auto-open the new drawing
    var newItem = allCharts.FirstOrDefault(c => c.Id == id);
    if (newItem != null)
    {
        await OpenDrawingAsync(newItem);
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

    Charts.Clear();
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