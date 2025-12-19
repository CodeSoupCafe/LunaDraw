namespace LunaDraw.Components.Carousel;

public abstract class ItemState : ISortable
{
  public abstract Guid Id { get; }
  public abstract string Title { get; }
  public abstract DateTimeOffset DateCreated { get; }
  public abstract DateTimeOffset DateUpdated { get; }

  public override abstract bool Equals(object? other);
  public override abstract int GetHashCode();
}
