using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Commands
{
    /// <summary>
    /// Command to remove an element from a layer.
    /// </summary>
    public class RemoveElementCommand : IDrawCommand
    {
        private readonly Layer _layer;
        private readonly IDrawableElement _element;
        private int _index;

        public RemoveElementCommand(Layer layer, IDrawableElement element)
        {
            _layer = layer;
            _element = element;
        }

        public void Execute()
        {
            _index = _layer.Elements.IndexOf(_element);
            _layer.Elements.Remove(_element);
        }

        public void Undo()
        {
            _layer.Elements.Insert(_index, _element);
        }
    }
}
