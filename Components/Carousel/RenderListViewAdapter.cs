namespace LunaDraw.Components.Carousel;

using AsyncAwaitBestPractices.MVVM;
using BurnCalc.State.Models;
using BurnCalc.State.Thunks;
using BurnCalc.Views.Adapters;
using BurnCalc.Views.Pages.ViewModels;
using BurnCalc.Views.Types;
using CodeSoupCafe.Xamarin.Extensions;
using CodeSoupCafe.Xamarin.Messaging;
using CodeSoupCafe.Xamarin.Views.Navigation;
using CodeSoupCafe.Xamarin.Views.Selectors;
using CodeSoupCafe.Xamarin.Views.Types;
using Microsoft.AppCenter.Crashes;
using Redux;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using static BurnCalc.Resources.Constants;

public interface IChartListView
{
  CollectionView ChartCollectionView { get; }
  ListView ChartList { get; }
  GridItemsLayout MediaItemGridLayout { get; }
  MoreMenuNavigation MoreMenuNavRef { get; }
  StackLayout SortPanel { get; }
  StackLayout SearchPanel { get; }
  Entry SearchEntry { get; }
  ChartRenderListViewModel ViewModel { get; }
}

public class RenderListViewAdapter : ChartRenderListViewModel
{
  private readonly ActionThunks actionThunks;
  private readonly IKeyboardHider keyboardHider;
  private readonly double scrollAllowImageUpdateRange = 5;
  private readonly int scrollAllowUpdateRange = 120;
  private readonly int dropdownOutOfBoundsY = -120;
  private readonly List<IDisposable> subscriptions = new List<IDisposable>();
  private readonly Subject<object?> componentTriggered = new Subject<object?>();
  private IChartListView ChartListView;
  private IDisposable? refreshFromScroll;
  private IDisposable? refreshImagesFromScroll;
  private double lastScrollValue;
  private bool isSubscribed;
  public bool IsPaused { get; set; }

  public RenderListViewAdapter()
  {
    ChartCommands = Locator.Current.GetService<ChartViewCommandAdapter>().IsNullReferenceCheck<ChartViewCommandAdapter>();
    actionThunks = Locator.Current.GetService<ActionThunks>().IsNullReferenceCheck<ActionThunks>();
    keyboardHider = DependencyService.Get<IKeyboardHider>();

    IsPaused = false;
  }

  public ChartViewCommandAdapter ChartCommands { get; set; }

  //public List<DynamicTemplateData> MoreMenuItems => new List<DynamicTemplateData> {
  //  new DynamicTemplateData(nameof(NavigationIconViewCell), new NavigationLink
  //  {
  //    Title = AppStrings.AddNew,
  //    Icon = IconStrings.IconGroupAdd,
  //    Layout = StackOrientation.Horizontal,
  //    Command = ChartCommands.AddNewChartCommand
  //  }),
  //  new DynamicTemplateData(nameof(NavigationIconViewCell), new NavigationLink
  //  {
  //    Title = AppStrings.Refresh,
  //    Icon = IconStrings.IconRefresh,
  //    Layout = StackOrientation.Horizontal,
  //    Command = ChartListView.ViewModel.ReloadChartData
  //  }),
  //  new DynamicTemplateData(nameof(NavigationIconViewCell), new NavigationLink
  //  {
  //    Title = AppStrings.ResetCompare,
  //    Icon = IconStrings.IconCompareArrows,
  //    Layout = StackOrientation.Horizontal,
  //    Command = ChartListView.ViewModel.ResetChartData
  //  })
  //};

  public List<DynamicTemplateData> NavigationPanels => new List<DynamicTemplateData> {
      new DynamicTemplateData(nameof(SortFilterViewCell), null)
    };

  public IAsyncCommand<ViewType> ShowGrid =>
    new AsyncCommand<ViewType>(async (viewType) =>
    {
      await ChartListView.MoreMenuNavRef.ToggleVisibility.ExecuteAsync(false);
      ChartListView.ViewModel.IsGridMode = viewType == ViewType.Grid;

      App.Preferences.ChartViewType = viewType;
    }, (x) => true);

  public IAsyncCommand ShowHideSortPanelCommand =>
    new AsyncCommand(async () =>
    {
      await Device.InvokeOnMainThreadAsync(async () =>
      {
        await ChartListView.MoreMenuNavRef.ToggleVisibility.ExecuteAsync(false);

        if (ChartListView.SortPanel.IsVisible)
        {
          await HideSortPanel();
        }
        else
        {
          if (ChartListView.SearchPanel.IsVisible)
          {
            ChartListView.SearchPanel.IsVisible = false;

            keyboardHider?.HideKeyboard();
          }

          await ShowSortPanel();
        }
      });
    });

  private async Task ShowSortPanel()
  {
    ChartListView.SortPanel.IsVisible = true;

    _ = await ChartListView.SortPanel
      .TranslateTo(0, 0, 190, Easing.SinOut);
  }

  private async Task HideSortPanel()
  {
    await ChartListView.SortPanel
      .TranslateTo(0, dropdownOutOfBoundsY, 320, Easing.SinIn)
      .ContinueWith(task =>
      {
        if (task.IsCompletedSuccessfully)
          Device.BeginInvokeOnMainThread(() =>
          {
            ChartListView.SortPanel.IsVisible = false;
          });
      });
  }

  public StackLayout SortPanel { get; private set; }

  private bool isClearCommand = false;

  public ICommand ClearSearchPanelCommand =>
    new Command<bool>((closeAfter) =>
    {
      isClearCommand = true;

      Device.BeginInvokeOnMainThread(async () =>
      {
        ChartListView.SearchEntry.Text = "";
        _ = ChartListView.SearchEntry.Focus();

        if (closeAfter)
        {
          await HideSearchPanel();
          //keyboardHider?.HideKeyboard();
        }
      });
    });

  public IAsyncCommand ShowHideSearchPanelCommand =>
    new AsyncCommand(async () =>
    {
      await Device.InvokeOnMainThreadAsync(async () =>
      {
        await ChartListView.MoreMenuNavRef.ToggleVisibility.ExecuteAsync(false);

        if (ChartListView.SearchPanel.IsVisible)
        {
          await HideSearchPanel();
        }
        else
        {
          if (ChartListView.SortPanel.IsVisible)
            ChartListView.SortPanel.IsVisible = false;

          await ShowSearchPanel();

          ChartListView.SearchEntry.Focus();
        }
      });
    });

  private async Task ShowSearchPanel()
  {
    ChartListView.SearchPanel.IsVisible = true;

    await ChartListView.SearchPanel
      .TranslateTo(0, 0, 190, Easing.SinOut);
  }

  private async Task HideSearchPanel()
  {
    await ChartListView.SearchPanel
      .TranslateTo(0, dropdownOutOfBoundsY, 320, Easing.SinIn)
      .ContinueWith(task =>
      {
        if (task.IsCompletedSuccessfully)
          Device.BeginInvokeOnMainThread(() =>
          {
            ChartListView.SearchPanel.IsVisible = false;
          });
      });

    keyboardHider?.HideKeyboard();
  }

  public StackLayout SearchPanel { get; private set; }

  public void ChartCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    ChartListView.MoreMenuNavRef.ToggleVisibility.Execute(false);

    if (ChartListView.ChartCollectionView.SelectedItem is ChartState chartState)
    {
      ChartListView.ChartCollectionView.SelectedItem = null;
      ChartCommands.LoadChartTabbedViewCommand.Execute(chartState);
    }
  }

  public void ChartGrid_Scrolled(object sender, ItemsViewScrolledEventArgs e)
  {
    refreshImagesFromScroll?.Dispose();
    refreshImagesFromScroll = null;

    refreshImagesFromScroll = new Action<Unit>((x) =>
    {
      if (lastScrollValue == e.VerticalDelta)
        _ = GlobalBroadcaster.Broadcast(AppMessageStateType.ImageLoadingState, new ImageLoadingState(ImageLoadingType.ForceRedraw));
    }).Debounce(ReactiveTiming.RenderCanvasLoadResetLongerDelay * 2);

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

        if (ChartListView?.MoreMenuNavRef?.IsVisible ?? false)
          ChartListView?.MoreMenuNavRef?.ToggleVisibility.Execute(false);

        Device.BeginInvokeOnMainThread(async () =>
        {
          await HideSortPanel();
          await HideSearchPanel();
        });
      }).Debounce(ReactiveTiming.RenderCanvasLoadResetLongerDelay);
    }
  }

  public void ListView_ItemTapped(object sender, ItemTappedEventArgs e)
  {
    ChartListView.ChartList.SelectedItem = null;
    ChartCommands.LoadChartTabbedViewCommand.Execute(e.Item);
  }

  public void OnAppearing(IChartListView chartListView)
  {
    IsPaused = false;

    ChartListView = chartListView;

    Device.BeginInvokeOnMainThread(async () =>
    {
      ChartListView.MoreMenuNavRef.OnAppearing();

      if (!isSubscribed)
      {
        isSubscribed = true;

        CreateSubscriptions();
      }

      await HideSearchPanel();
      await HideSortPanel();

      //_ = GlobalBroadcaster.Broadcast(AppMessageStateType.ImageLoadingState, new ImageLoadingState(ImageLoadingType.IsLoading), ReactiveTiming.ChartListOrientationCanvasDelay);
    });
  }

  public void OnDisappearing()
  {
    ChartListView.MoreMenuNavRef.OnDisappearing();
    //ClearSubscriptions();
    IsPaused = true;
  }

  //private void ClearSubscriptions()
  //{
  //  subscriptions?.ForEach(x => x.Dispose());
  //  subscriptions?.Clear();
  //}
  private bool firstRun = false;
  private bool isLoadedCheck;

  private void CreateSubscriptions()
  {
    subscriptions.Add(App.AppStore.ObserveState()
      .Select(state => state.Charts)
      .DistinctUntilChanged()
      .Throttle(ReactiveTiming.ChartListUpdateDelay)
      .Subscribe(charts =>
      {
        try
        {
          ChartListView.ViewModel.SetCharts(charts.Values);
          ChartListView.SortPanel.TranslateTo(0, dropdownOutOfBoundsY);

          if (firstRun)
            return;

          firstRun = true;

          ChartListView.ViewModel.SortCharts();
          CalculateItemWidth();
        }
        catch (Exception ex)
        {
          Crashes.TrackError(ex);
        }
      }));

    subscriptions.Add(ChartListView.ViewModel.WhenPropertyChanged
      .Throttle(ReactiveTiming.ChartListUpdateSortDelay)
      .Where(x => x.StartsWith("ChartListSort"))
      .Subscribe(x =>
      {
        ChartListView.ViewModel.SortCharts();
      }));

    ChartListView.SearchEntry.Unfocused += SearchEntry_Unfocused;
  }

  private void SearchEntry_Focused(object sender, FocusEventArgs e)
  {
    Device.BeginInvokeOnMainThread(async () =>
    {
      await ShowSearchPanel();
    });
  }

  public void SearchEntry_Unfocused(object sender, FocusEventArgs e)
  {
    if (!isClearCommand)
      Device.BeginInvokeOnMainThread(async () =>
      {
        await HideSearchPanel();
      });

    isClearCommand = false;
  }

  public void CalculateItemWidth()
  {
    if (ChartListView != null)
      Device.BeginInvokeOnMainThread(() =>
      {
        var currentWidth = App.Instance?.MainPage?.Width ?? 200;

        if (ChartListView.ChartCollectionView.WidthRequest == currentWidth)
          return;

        var spanValue = Math.Clamp(Convert.ToInt32(currentWidth / 200), 1, 99);

        ChartListView.ChartCollectionView.WidthRequest = currentWidth;
        ChartListView.MediaItemGridLayout.Span = spanValue;
      });
  }
}