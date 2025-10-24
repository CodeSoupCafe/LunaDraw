using LunaDraw.Logic.Tools;

namespace LunaDraw.Logic.Messages
{
    /// <summary>
    /// Message sent when the active drawing tool changes.
    /// </summary>
    public class ToolChangedMessage
    {
        public IDrawingTool NewTool { get; }

        public ToolChangedMessage(IDrawingTool newTool)
        {
            NewTool = newTool;
        }
    }
}
