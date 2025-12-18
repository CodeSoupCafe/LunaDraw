namespace LunaDraw.Components.Carousel;

public class DataTemplateTyped<T> : DataTemplate
{
  public DataTemplateTyped() : base(typeof(T))
  {
  }
}