using LunaDraw.Logic.Models;
using System.Collections.Generic;
using System.Linq;

namespace LunaDraw.Logic.Commands
{
    /// <summary>
    /// Command to group multiple elements into a single DrawableGroup.
    /// </summary>
    public class GroupElementsCommand : IDrawCommand
    {
        private readonly Layer _layer;
        private readonly List<IDrawableElement> _elements;
        private DrawableGroup? _group;

        public GroupElementsCommand(Layer layer, IEnumerable<IDrawableElement> elements)
        {
            _layer = layer;
            _elements = elements.ToList();
        }

        public void Execute()
        {
            // Create a new group and add the elements to it
            _group = new DrawableGroup();
            foreach (var element in _elements)
            {
                _group.Children.Add(element);
                _layer.Elements.Remove(element);
            }
            _layer.Elements.Add(_group);
        }

        public void Undo()
        {
            if (_group == null) return;

            // Remove the group and add the elements back to the layer
            _layer.Elements.Remove(_group);
            foreach (var element in _elements)
            {
                _layer.Elements.Add(element);
            }
            _group = null;
        }
    }
}
