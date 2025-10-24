using LunaDraw.Logic.Models;
using SkiaSharp;

namespace LunaDraw.Logic.Commands
{
    /// <summary>
    /// Command to move an element by a specified offset.
    /// </summary>
    public class MoveElementCommand : IDrawCommand
    {
        private readonly IDrawableElement _element;
        private readonly SKPoint _offset;

        public MoveElementCommand(IDrawableElement element, SKPoint offset)
        {
            _element = element;
            _offset = offset;
        }

        public void Execute()
        {
            _element.Translate(_offset);
        }

        public void Undo()
        {
            _element.Translate(new SKPoint(-_offset.X, -_offset.Y));
        }
    }
}
