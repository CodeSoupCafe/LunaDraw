namespace LunaDraw.Logic.Messages;

public class DrawingListChangedMessage
{
  public Guid? DrawingId { get; }

  public DrawingListChangedMessage(Guid? drawingId = null)
  {
    DrawingId = drawingId;
  }
}
