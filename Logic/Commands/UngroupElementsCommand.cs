using LunaDraw.Logic.Models;
using System.Collections.Generic;
using System.Linq;

namespace LunaDraw.Logic.Commands
{
    /// <summary>
    /// Command to ungroup a DrawableGroup into its individual elements.
    /// </summary>
    public class UngroupElementsCommand : IDrawCommand
    {
        private readonly Layer _layer;
        private readonly DrawableGroup _group;
        private List<IDrawableElement> _elements;

        public UngroupElementsCommand(Layer layer, DrawableGroup group)
        {
            _layer = layer;
            _group = group;
            _elements = group.Children.ToList();
        }

        public void Execute()
        {
            // Remove the group and add its children back to the layer
            _layer.Elements.Remove(_group);
            foreach (var element in _elements)
            {
                _layer.Elements.Add(element);
            }
        }

        public void Undo()
        {
            // Remove the children from the layer and add the group back
            foreach (var element in _elements)
            {
                _layer.Elements.Remove(element);
            }
            _layer.Elements.Add(_group);
        }
    }
}
