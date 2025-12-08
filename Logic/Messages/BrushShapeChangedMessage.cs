using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Messages
{
    public class BrushShapeChangedMessage(BrushShape shape)
    {
        public BrushShape Shape { get; } = shape;
    }
}
