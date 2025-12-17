namespace LunaDraw.Components.Carousel;

using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class RenderListView : ContentPage, IChartListView, IDisposable, IEnableLogger
{
  private readonly List<IDisposable> subscriptions = new List<IDisposable>();
  private static IDisposable updateSubscription;

  private void ClearSubscriptions()
  {
    subscriptions?.ForEach(x => x.Dispose());
    subscriptions?.Clear();
  }

  public RenderListView()
  {
    if (BindingContext != this)
      BindingContext = this;

    InitializeComponent();

    ViewModel.IsLoading = true;
  }

  public string AdUnitId
  {
    get
    {
      if (Device.RuntimePlatform == Device.iOS)
#if DEBUG
        return "ca-app-pub-3940256099942544/2934735716";
#else
          return "ca-app-pub-9478090667872330/9763444222";
#endif
      else if (Device.RuntimePlatform == Device.Android)
      {
#if DEBUG
        return "ca-app-pub-3940256099942544/6300978111";
#else
          return "ca-app-pub-9478090667872330/4509069303";
#endif
      }

      return string.Empty;
    }
  }

  CollectionView IChartListView.ChartCollectionView => ChartCollectionView;
  ListView IChartListView.ChartList => ChartList;
  public RenderListViewAdapter RenderListViewAdapter { get; } = new RenderListViewAdapter();
  GridItemsLayout IChartListView.MediaItemGridLayout => MediaItemGridLayout;
  MoreMenuNavigation IChartListView.MoreMenuNavRef => MoreMenuNavRef;
  StackLayout IChartListView.SortPanel => SortPanel;
  StackLayout IChartListView.SearchPanel => SearchPanel;
  Entry IChartListView.SearchEntry => SearchEntry;
  public ChartRenderListViewModel ViewModel { get; } = new ChartRenderListViewModel();

  protected override void OnAppearing()
  {
    CreateSubscriptions();

    RenderListViewAdapter?.OnAppearing(this);

    NavigationAdapter.ShowNavigationBar();

    //try
    //{
    //  Device.BeginInvokeOnMainThread(async () =>
    //  {
    //    if (!await subscriber.IsSubscriber() ||
    //      !App.Preferences.IsEmailSubmitted)
    //    {
    //      App.Preferences.ShowBackgroundChart = false;
    //      App.Preferences.ShowDarkRegionLines = false;
    //    }
    //  });
    //}
    //catch (Exception ex)
    //{
    //  Crashes.TrackError(ex);
    //}

    _ = App.AppStore?.Dispatch(new State.Actions.AppActions.AskForEmailAtLeastOnce());
  }

  protected override void OnDisappearing()
  {
    RenderListViewAdapter?.OnDisappearing();
  }

  private void ChartGrid_Scrolled(object sender, ItemsViewScrolledEventArgs e) => RenderListViewAdapter?.ChartGrid_Scrolled(sender, e);

  private void CreateSubscriptions()
  {
    if (subscriptions.Any())
      return;

    subscriptions.Add(App.AppStore.ObserveState()
    .Select(x => x.AppUserModel)
    .Delay(ReactiveTiming.AndroidDelayLoadingAdsFirstLoad)
    .DistinctUntilChanged()
    .Subscribe((x) =>
    {
      Device.BeginInvokeOnMainThread(async () =>
      {
        var purchases = await inAppBillingAdapter.GetUserPurchases();

        ViewModel.IsAdsEnabled = AdMobElement.IsVisible = purchases == null || purchases.Count() == 0;
      });
    }));

    subscriptions.Add(App.AppStore.ObserveState()
      .Select(x => x.IsLoading)
      .DistinctUntilChanged()
      .Throttle(ReactiveTiming.ChartListUpdateDelay)
      .Subscribe(x => ViewModel.IsLoading = ViewModel.IsRefreshing = x));

    subscriptions.Add(GlobalBroadcaster.Subscribe<AppLoadPercent>(this, AppMessageStateType.ThreadMonitorState, async appLoadPercent =>
    {
      if (ProgressIndicator == null)
        return;

      if (appLoadPercent.LoadPercent > 0)
      {
        ActivityIndicator.IsVisible = false;
        ProgressIndicator.IsVisible = true;
      }

      if (appLoadPercent.LoadPercent == 1 ||
        (!App.AppStore?.CurrentState.IsLoading ?? false))
      {
        ActivityIndicator.IsVisible = false;
        ProgressIndicator.IsVisible = false;
      }
      else
        _ = await ProgressIndicator.ProgressTo(appLoadPercent.LoadPercent, 500, Easing.Linear);
    }));

    LoadApp();
  }

  private void ListView_ItemTapped(object sender, ItemTappedEventArgs e) => RenderListViewAdapter?.ListView_ItemTapped(sender, e);

  private void SearchEntry_TextChanged(object sender, TextChangedEventArgs e)
  {
    SortCharts(e.NewTextValue);
  }

  private void SortCharts(string newValue)
  {
    try
    {
      if (!RenderListViewAdapter.IsPaused)
      {
        updateSubscription?.Dispose();
        updateSubscription = new Action<Unit>((x) =>
        {
          Device.BeginInvokeOnMainThread(() =>
          {
            var filteredResults = App.AppStore?.CurrentState.Charts.Values.Where(ViewModel.FilterCharts)?.ToList();

            ChartRenderListViewModel.UpdateCharts(ViewModel.Charts, filteredResults, () => ViewModel.SortCharts());

            Title = string.IsNullOrWhiteSpace(newValue) ? AppStrings.Charts : $"{filteredResults?.Count() ?? 0} {AppStrings.Results}: {newValue}";
          });
        }).Debounce(ReactiveTiming.ChartListUpdateSortDelay);
      }
    }
    catch
    {
      Title = string.IsNullOrWhiteSpace(newValue) ? AppStrings.Charts : $"0 {AppStrings.Results}: {newValue}";
    }
  }

  #region IDisposable Support

  private bool disposedValue = false; // To detect redundant calls

  // This code added to correctly implement the disposable pattern.
  public void Dispose()
  {
    // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    Dispose(true);
    // TODO: uncomment the following line if the finalizer is overridden above.
    // GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!disposedValue)
    {
      if (disposing)
      {
        // TODO: dispose managed state (managed objects).
        ClearSubscriptions();
      }

      // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
      // TODO: set large fields to null.

      disposedValue = true;
    }
  }

  // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
  // ~ChartListView()
  // {
  //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
  //   Dispose(false);
  // }

  #endregion IDisposable Support

  private void AdMobContainer_SizeChanged(object sender, EventArgs e)
  {
    RenderListViewAdapter?.CalculateItemWidth();
    _ = GlobalBroadcaster.Broadcast(AppMessageStateType.ImageLoadingState, new ImageLoadingState(ImageLoadingType.ForceRedraw));
  }
}