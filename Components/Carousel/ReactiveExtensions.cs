using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace LunaDraw.Components.Carousel;

public static class ReactiveExtensions
{
  public static TimeSpan DefaultDelayInMilliseconds = TimeSpan.FromMilliseconds(80);

  public static IDisposable Debounce(this Action<Unit> action, TimeSpan? timeDelay = null)
  {
    return Observable.Create<Unit>(x =>
    {
      x.OnNext(Unit.Default);
      return Disposable.Create(() => { });
    })
    .DistinctUntilChanged()
    .Throttle(timeDelay ?? DefaultDelayInMilliseconds)
    .Subscribe(action);
  }
}