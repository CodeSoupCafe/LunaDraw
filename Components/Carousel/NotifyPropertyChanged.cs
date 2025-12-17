namespace LunaDraw.Components.Carousel;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

public abstract class NotifyPropertyChanged : INotifyPropertyChanged
{
  event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
  {
    add => PropertyChanged += value;
    remove => PropertyChanged -= value;
  }

  private event PropertyChangedEventHandler? PropertyChanged;

  protected bool SetProperty<T>(ref T backingStore, T value,
    [CallerMemberName] string propertyName = "",
    Action? onChanged = null)
  {
    if (EqualityComparer<T>.Default.Equals(backingStore, value))
      return false;

    backingStore = value;

    onChanged?.Invoke();
    OnPropertyChanged(propertyName);

    return true;
  }

  public IObservable<string> WhenPropertyChanged
  {
    get => Observable
      .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
        h => PropertyChanged += h,
        h => PropertyChanged -= h)
      .Select(x => x.EventArgs.PropertyName ?? "");
  }

  protected void OnPropertyChanged(string propertyName) =>
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}