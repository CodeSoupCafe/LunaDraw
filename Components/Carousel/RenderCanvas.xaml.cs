namespace LunaDraw.Components.Carousel
{
  using LunaDraw.Logic.Models;
  using CodeSoupCafe.Xamarin.Extensions;
  using SkiaSharp;
  using SkiaSharp.Views.Maui;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reactive.Linq;
  using System.Reactive.Subjects;

  public abstract class ItemState : IEquatable<object>, ISortable
  {
    public abstract DateTimeOffset DateCreated { get; }
    public abstract DateTimeOffset DateUpdated { get; }

    public Dictionary<string, object?> Properties = [];

    public object? this[string key]
    {
      get
      {
        return default;
      }
    }

    /// <summary>
    /// TODO: Use reflection to implement the abstract Equals and GetHashCode in the inherited class
    /// 
    /// </summary>
    /// <returns></returns>
    public override abstract bool Equals(object? other);
    public override abstract int GetHashCode();
  }

  public class RenderViewModel : NotifyPropertyChanged
  {
    internal static TimeSpan RenderCanvasTriggerChartUpdateDelay = TimeSpan.FromMilliseconds(187);
    private string? id;
    private ItemState? state;
    private bool isLoading;

    public string? Id
    {
      get => id;
      set => SetProperty(ref id, value);
    }

    public bool IsLoading
    {
      get => isLoading;
      set => SetProperty(ref isLoading, value);
    }
    public ItemState? ChartState
    {
      get => state;
      set => SetProperty(ref state, value);
    }
  }

  public partial class RenderCanvas : ContentView, IDisposable
  {
    public static readonly BindableProperty ViewModelProperty =
       BindableProperty.Create(nameof(ViewModel), typeof(RenderViewModel), typeof(RenderCanvas), propertyChanged: OnViewModelChanged);

    private readonly Subject<bool> resetTriggered = new Subject<bool>();
    private readonly List<IDisposable> subscriptions = new List<IDisposable>();
    public SKImage? Image;
    private List<External.Drawing>? drawing;
    private int? establishedWidth, establishedHeight = 0;

    public RenderCanvas()
    {
      InitializeComponent();

      try
      {
        CanvasViewRef?.PaintSurface += OnCanvasViewPaintSurface;

        CreateSubscriptions();
      }
      catch // (Exception ex)
      {
        // Crashes.TrackError(ex);
      }
    }

    public IObservable<bool> ResetTriggered => resetTriggered.AsObservable();

    public RenderViewModel ViewModel
    {
      get => (RenderViewModel)GetValue(ViewModelProperty);
      set => SetValue(ViewModelProperty, value);
    }


    public void OnCanvasViewPaintSurface(object? sender, SKPaintSurfaceEventArgs args)
    {
      try
      {
        //if (Image != null)
        //{
        //  var screenBounds = new SKRect(0, 0, args.Info.Width, args.Info.Height);
        //  var imageBounds = SKMatrix.CreateIdentity().MapRect(screenBounds);

        //  imageBounds = imageBounds.AspectFitFill(Image.Width, Image.Height);
        //  args.Surface?.Canvas.DrawImage(Image, imageBounds);

        //  return;
        //}

        if (isLoaded ||
          args?.Surface?.Canvas == null ||
          drawing == null ||
          ViewModel?.Id == null)
          return;

        SKSurface? surface = args.Surface;
        surface?.Canvas?.Clear(BackgroundColor.ToSKColor());

        ViewModel.IsLoading = true;

        var pathBounds = new SKPath();

        var bounds = pathBounds?.Bounds.AspectFill(new SKSize(args.Info.Width * 0.9f, args.Info.Height * 0.9f));

        if (bounds == null)
        {
          var opacityPaint = LibraryExtensions.BasePaint.AsOpacity(10);
          opacityPaint.Style = SKPaintStyle.Fill;

          surface?.Canvas?.DrawRect(0, 0, args.Info.Width, args.Info.Height, opacityPaint);

          return;
        }

        surface?.Canvas?.MaxScaleCentered(
          Convert.ToInt32(args.Info.Width * 0.95),
          Convert.ToInt32(args.Info.Height * 0.95),
          bounds.Value,
          Convert.ToInt32(args.Info.Width * 0.05),
          Convert.ToInt32(args.Info.Height * 0.05),
          1);

        if (surface?.Canvas == null)
          return;

        // surface?.RenderChart(new RenderChartModel(ViewModel.ChartId, ViewModel.CollectionType, drawing));
        surface?.Canvas?.Flush();

        isLoaded = true;

        if (Image != null)
          Image.Dispose();

        Image = surface?.Snapshot();
      }
      catch  //(Exception ex)
      {
        // Crashes.TrackError(ex);
      }
      finally
      {
        ViewModel.IsLoading = false;
      }
    }

    private static void OnGenericPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
      if (oldValue == newValue ||
        newValue == null) return;

      var self = (RenderCanvas)bindable;
      self.isLoaded = false;

      self.resetTriggered.OnNext(false);
    }

    private static void OnViewModelChanged(BindableObject bindable, object oldValue, object newValue)
    {
      if (newValue is RenderViewModel _)
      {
        var self = (RenderCanvas)bindable;

        self.isLoaded = false;
        self.CreateViewModelSubs(self);
        self.resetTriggered.OnNext(false);
      }

      return;
    }

    private void CreateSubscriptions()
    {
      subscriptions.Add(ResetTriggered
       .Subscribe(x => LoadAndReset()));
    }

    private bool isViewModelSubsSet;

    private void CreateViewModelSubs(RenderCanvas self)
    {
      if (isViewModelSubsSet)
        return;

      isViewModelSubsSet = true;

      self.subscriptions.Add(self.ViewModel.WhenPropertyChanged
        .Where(x => x == nameof(ItemState))
        .Throttle(RenderViewModel.RenderCanvasTriggerChartUpdateDelay)
        .Subscribe(propertyName =>
        {
          self.isLoaded = false;
          self.resetTriggered.OnNext(false);
        }));

      self.subscriptions.Add(GlobalBroadcaster.Subscribe<ImageLoadingState>(self,
        AppMessageStateType.ImageLoadingState,
        (_, imageLoadingState) =>
        {
          switch (imageLoadingState.LoadingState)
          {
            case ImageLoadingType.IsLoading:
              self.resetTriggered.OnNext(false);
              break;
            case ImageLoadingType.ForceRedraw:
              self.isLoaded = false;
              self.Image?.Dispose();
              self.Image = null;

              self.resetTriggered.OnNext(false);
              break;
          }
        }));
    }

    private void LoadAndReset()
    {
      if (isDisposed
        || ViewModel?.ChartState?.Properties == null)
        return;

      MainThread.BeginInvokeOnMainThread(() =>
      {
        if (isDisposed)
          return;

        if (CanvasViewRef != null)
          try
          {
            CanvasViewRef?.InvalidateSurface();
          }
          catch// (Exception ex)
          {
            //            Crashes.TrackError(ex);
          }
      });
    }

    #region IDisposable Support

    private bool disposedValue = false; // To detect redundant calls
    private bool isDisposed;
    private bool isLoaded;

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
          try
          {
            // TODO: dispose managed state (managed objects).
            subscriptions?.ForEach(x => x.Dispose());
            subscriptions?.Clear();

            CanvasViewRef.PaintSurface -= OnCanvasViewPaintSurface;

            isDisposed = true;
          }
          catch //(Exception ex)
          {
            // Crashes.TrackError(ex);
          }
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.

        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~RenderCanvas()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      //Dispose(false);
    }

    #endregion IDisposable Support
  }
}