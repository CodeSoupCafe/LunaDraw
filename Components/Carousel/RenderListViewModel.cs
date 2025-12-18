namespace LunaDraw.Components.Carousel;

using LunaDraw.Logic.Utils;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;

public class RenderListViewModel : NotifyPropertyChanged, IEnableLogger
{
  private bool isEmptyCharts;
  private bool isLoading = true;
  private bool isRefreshing;
  private bool isAdsEnabled;
  private bool? isGridMode;
  private string? chartListSortOrder;
  private string? chartListSortProperty;
  private string? userName;
  private string searchText;
  private readonly IPreferencesFacade prefernecesFacade;

  public RenderListViewModel(IPreferencesFacade prefernecesFacade)
  {
    this.prefernecesFacade = prefernecesFacade;
  }

  public string ChartListSortOrder
  {
    get => prefernecesFacade.Get(AppPreference.ListSortOrder);
    set
    {
      if (string.IsNullOrWhiteSpace(value) || value == prefernecesFacade.Get(AppPreference.ListSortOrder))
        return;

      if (Enum.TryParse<SortOrder>(value, out var selectedItem))
        prefernecesFacade.Set(AppPreference.ListSortOrder, selectedItem);

      SetProperty(ref chartListSortOrder, value);
    }
  }

  public string ChartListSortProperty
  {
    get => prefernecesFacade.Get(AppPreference.ListSortProperty);
    set
    {
      if (string.IsNullOrWhiteSpace(value) ||
        value == prefernecesFacade.Get(AppPreference.ListSortProperty))
        return;

      if (Enum.TryParse<SortProperty>(value, out var selectedItem))
        prefernecesFacade.Set(AppPreference.ListSortProperty, selectedItem);

      SetProperty(ref chartListSortProperty, value);
    }
  }

  public bool IsEmptyCharts
  {
    get => isEmptyCharts;
    set => SetProperty(ref isEmptyCharts, value);
  }

  public bool IsGridMode
  {
    get => isGridMode ?? prefernecesFacade.Get<bool>(AppPreference.IsListGridView);
    set => SetProperty(ref isGridMode, value);
  }

  public string SearchText
  {
    get => searchText;
    set => SetProperty(ref searchText, value);
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

  public ICommand ReloadChartData =>
    new Command(() =>
    {
      // App.AppStore.Dispatch(new ChartActions.SyncCharts());
    });

  public List<string> SortOrders { get; } = ((SortOrder[])Enum.GetValues(typeof(SortOrder))).Skip(1).Select(x => x.ToString()).ToList();

  public List<string> SortProperties { get; } = ((SortProperty[])Enum.GetValues(typeof(SortProperty))).Skip(1).Select(x => x.ToString()).ToList();

  public void SetCharts(IEnumerable<ItemState> charts)
  {
    try
    {
      IsLoading = true;
      IsEmptyCharts = false;

      if (charts == null ||
        !charts.Any())
      {
        // Charts.Clear();

        IsEmptyCharts = true;

        ShowWelcomeView();

        return;
      }

      // UpdateCharts(Charts, charts.Where(FilterCharts)?.ToList(), () => SortCharts());
    }
    catch (Exception ex)
    {
      // this.Log().Error(nameof(ChartRenderListViewModel), ex);
    }
    finally
    {
      IsRefreshing = false;
      IsLoading = false;
    }
  }

  public bool FilterCharts(ItemState? chartState)
  {
    if (string.IsNullOrWhiteSpace(SearchText))
    {
      return true;
    }

    if (chartState?.Title == null)
    {
      return false;
    }

    return chartState.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
  }

  public static void UpdateCharts(
    RangedObservableCollection<ItemState> charts,
    List<ItemState>? newCharts = null,
    Action? afterAction = null)
  {
    try
    {
      var itemsToRemove = new Stack<ItemState>();
      var chartCount = charts.Count;
      var filteredCharts = newCharts ??= [];

      for (int chartIndex = 0; chartIndex < chartCount; chartIndex++)
      {
        ItemState currentChart = charts[chartIndex];

        int newChartIndex = filteredCharts.FindIndex(x => x.Id == currentChart.Id);

        if (newChartIndex == -1)
        {
          itemsToRemove.Push(currentChart);
        }
        else if (!currentChart.Equals(filteredCharts[newChartIndex]))
        {
          charts[chartIndex] = filteredCharts[newChartIndex];
        }
      }

      if (itemsToRemove.Any())
        foreach (ItemState itemToRemove in itemsToRemove)
          charts.Remove(itemToRemove);

      foreach (ItemState itemToAdd in filteredCharts)
        if (!charts.Any(x => x.Id == itemToAdd.Id))
          charts.Add(itemToAdd);

      afterAction?.Invoke();
    }
    catch (Exception ex)
    {
      // Crashes.TrackError(ex);
    }
  }

  public void SortCharts()
  {
    // try
    // {
    //   Charts.Sort(Comparer<ChartState>.Create((x, y) =>
    //   {
    //     var firstItem = (DateTimeOffset)x.GetType().GetProperty(ChartListSortProperty).GetValue(x);
    //     var secondItem = (DateTimeOffset)y.GetType().GetProperty(ChartListSortProperty).GetValue(y);

    //     return ChartListSortOrder == SortOrder.Ascending.ToString() ?
    //       firstItem.CompareTo(secondItem) :
    //       secondItem.CompareTo(firstItem);
    //   }));
    // }
    // catch { }
  }

  private void ShowWelcomeView()
  {
    // Firstname = AppStrings.WelcomeUser.Replace(new Dictionary<string, object> {
    //     { nameof(Firstname), App.AppStore.CurrentState.AppUserModel?.Firstname ?? AppStrings.User }
    //   });

    // HasUser = !string.IsNullOrWhiteSpace(Firstname);
  }
}