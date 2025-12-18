using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Messages;

public class OpenDrawingMessage
{
    public External.Drawing Drawing { get; }
    public OpenDrawingMessage(External.Drawing drawing)
    {
        Drawing = drawing;
    }
}
