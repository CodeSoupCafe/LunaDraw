using SkiaSharp;

namespace LunaDraw.Logic.Utils
{
    public class QuadTree<T> where T : class
    {
        private readonly int _maxObjects = 10;
        private readonly int _maxLevels = 5;

        private readonly int _level;
        private readonly List<T> _objects;
        private readonly SKRect _bounds;
        private readonly Func<T, SKRect> _getBounds;
        private QuadTree<T>[]? _nodes;

        public QuadTree(int level, SKRect bounds, Func<T, SKRect> getBounds)
        {
            _level = level;
            _bounds = bounds;
            _getBounds = getBounds;
            _objects = new List<T>();
        }

        public void Clear()
        {
            _objects.Clear();

            if (_nodes != null)
            {
                foreach (var node in _nodes)
                {
                    node.Clear();
                }
                _nodes = null;
            }
        }

        private void Split()
        {
            float subWidth = _bounds.Width / 2f;
            float subHeight = _bounds.Height / 2f;
            float x = _bounds.Left;
            float y = _bounds.Top;

            _nodes = new QuadTree<T>[4];
            _nodes[0] = new QuadTree<T>(_level + 1, new SKRect(x + subWidth, y, x + subWidth + subWidth, y + subHeight), _getBounds);
            _nodes[1] = new QuadTree<T>(_level + 1, new SKRect(x, y, x + subWidth, y + subHeight), _getBounds);
            _nodes[2] = new QuadTree<T>(_level + 1, new SKRect(x, y + subHeight, x + subWidth, y + subHeight + subHeight), _getBounds);
            _nodes[3] = new QuadTree<T>(_level + 1, new SKRect(x + subWidth, y + subHeight, x + subWidth + subWidth, y + subHeight + subHeight), _getBounds);
        }

        /*
         * Index of the quadrant the object belongs to
         */
        private int GetIndex(SKRect pRect)
        {
            int index = -1;
            double verticalMidpoint = _bounds.Left + (_bounds.Width / 2f);
            double horizontalMidpoint = _bounds.Top + (_bounds.Height / 2f);

            bool topQuadrant = (pRect.Top < horizontalMidpoint && pRect.Bottom < horizontalMidpoint);
            bool bottomQuadrant = (pRect.Top > horizontalMidpoint);

            if (pRect.Left < verticalMidpoint && pRect.Right < verticalMidpoint)
            {
                if (topQuadrant)
                {
                    index = 1;
                }
                else if (bottomQuadrant)
                {
                    index = 2;
                }
            }
            else if (pRect.Left > verticalMidpoint)
            {
                if (topQuadrant)
                {
                    index = 0;
                }
                else if (bottomQuadrant)
                {
                    index = 3;
                }
            }

            return index;
        }

        public void Insert(T pObject)
        {
            if (_nodes != null)
            {
                int index = GetIndex(_getBounds(pObject));

                if (index != -1)
                {
                    _nodes[index].Insert(pObject);
                    return;
                }
            }

            _objects.Add(pObject);

            if (_objects.Count > _maxObjects && _level < _maxLevels)
            {
                if (_nodes == null)
                {
                    Split();
                }

                int i = 0;
                while (i < _objects.Count)
                {
                    int index = GetIndex(_getBounds(_objects[i]));
                    if (index != -1)
                    {
                        _nodes[index].Insert(_objects[i]);
                        _objects.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        public List<T> Retrieve(List<T> returnObjects, SKRect pRect)
        {
            int index = GetIndex(pRect);
            if (index != -1 && _nodes != null)
            {
                _nodes[index].Retrieve(returnObjects, pRect);
            }
            else if (_nodes != null)
            {
                // If the rect doesn't fit into a specific quadrant (overlaps multiple),
                // we must query all quadrants that it touches.
                // Simplified: query all subnodes if we can't determine a single one.
                // Or strictly check intersection.
                // For now, naive approach: if it doesn't fit one, retrieve from all.
                foreach (var node in _nodes)
                {
                    // Optimization: check intersection with node bounds
                    if (node._bounds.IntersectsWith(pRect))
                    {
                        node.Retrieve(returnObjects, pRect);
                    }
                }
            }

            returnObjects.AddRange(_objects);

            return returnObjects;
        }
    }
}
