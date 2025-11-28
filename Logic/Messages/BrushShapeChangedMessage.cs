using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Messages
{
    public class BrushShapeChangedMessage
    {
        public BrushShape Shape { get; }
        public BrushShapeChangedMessage(BrushShape shape)
        {
            Shape = shape;
        }
    }
}
