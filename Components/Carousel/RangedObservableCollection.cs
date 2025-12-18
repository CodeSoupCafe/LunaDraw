namespace LunaDraw.Components.Carousel;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

public class RangedObservableCollection<T> : ObservableCollection<T>
{
  private bool suppressNotification = false;

  protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
  {
    if (!suppressNotification)
      base.OnCollectionChanged(e);
  }

  public void ClearAndStaySilent()
  {
    suppressNotification = true;
    Clear();
  }

  public void AddRange(IEnumerable<T> items)
  {
    if (items == null)
    {
      Clear();
    }
    else
    {

      suppressNotification = true;

      foreach (var item in items)
        Add(item);

      suppressNotification = false;
    }

    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
  }
}