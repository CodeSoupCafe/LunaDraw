
namespace LunaDraw.Components.Carousel;

using System;

internal interface IDateNow
{
  DateTimeOffset DateCreated { get; }

  DateTimeOffset DateUpdated { get; }
}
