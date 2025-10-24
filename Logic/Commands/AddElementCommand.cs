using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Commands
{
    /// <summary>
    /// Command to add an element to a layer.
    /// </summary>
    public class AddElementCommand : IDrawCommand
    {
        private readonly Layer _layer;
        private readonly IDrawableElement _element;

        public AddElementCommand(Layer layer, IDrawableElement element)
        {
            _layer = layer;
            _element = element;
        }

        public void Execute()
        {
            _layer.Elements.Add(_element);
        }

        public void Undo()
        {
            _layer.Elements.Remove(_element);
        }
    }
}
