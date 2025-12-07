using LunaDraw.Logic.Tools;

namespace LunaDraw.Logic.Messages
{
    /// <summary>
    /// Message sent when the active drawing tool changes.
    /// </summary>
    public class ToolChangedMessage(IDrawingTool newTool)
    {
        public IDrawingTool NewTool { get; } = newTool;
    }
}
