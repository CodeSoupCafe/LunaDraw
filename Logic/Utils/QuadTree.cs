using SkiaSharp;

namespace LunaDraw.Logic.Utils
{
    public class QuadTree<T>(int level, SKRect bounds, Func<T, SKRect> getBounds) where T : class
    {
        private readonly int maxObjects = 10;
        private readonly int maxLevels = 5;

        private readonly int level = level;
        private readonly List<T> objects = [];
        private readonly SKRect bounds = bounds;
        private readonly Func<T, SKRect> getBounds = getBounds;
        private QuadTree<T>[]? nodes;

        public void Clear()
        {
            objects.Clear();

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    node.Clear();
                }
                nodes = null;
            }
        }

        private void Split()
        {
            float subWidth = bounds.Width / 2f;
            float subHeight = bounds.Height / 2f;
            float x = bounds.Left;
            float y = bounds.Top;

            nodes = new QuadTree<T>[4];
            nodes[0] = new QuadTree<T>(level + 1, new SKRect(x + subWidth, y, x + subWidth + subWidth, y + subHeight), getBounds);
            nodes[1] = new QuadTree<T>(level + 1, new SKRect(x, y, x + subWidth, y + subHeight), getBounds);
            nodes[2] = new QuadTree<T>(level + 1, new SKRect(x, y + subHeight, x + subWidth, y + subHeight + subHeight), getBounds);
            nodes[3] = new QuadTree<T>(level + 1, new SKRect(x + subWidth, y + subHeight, x + subWidth + subWidth, y + subHeight + subHeight), getBounds);
        }

        /*
         * Index of the quadrant the object belongs to
         */
        private int GetIndex(SKRect pRect)
        {
            int index = -1;
            double verticalMidpoint = bounds.Left + (bounds.Width / 2f);
            double horizontalMidpoint = bounds.Top + (bounds.Height / 2f);

            bool topQuadrant = pRect.Top < horizontalMidpoint && pRect.Bottom < horizontalMidpoint;
            bool bottomQuadrant = pRect.Top > horizontalMidpoint;

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
            if (nodes != null)
            {
                int index = GetIndex(getBounds(pObject));

                if (index != -1)
                {
                    nodes[index].Insert(pObject);
                    return;
                }
            }

            objects.Add(pObject);

            if (objects.Count > maxObjects && level < maxLevels)
            {
                if (nodes == null)
                {
                    Split();
                }

                int i = 0;
                while (i < objects.Count)
                {
                    int index = GetIndex(getBounds(objects[i]));
                    if (index != -1)
                    {
                        nodes![index].Insert(objects[i]);
                        objects.RemoveAt(i);
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
            if (index != -1 && nodes != null)
            {
                nodes[index].Retrieve(returnObjects, pRect);
            }
            else if (nodes != null)
            {
                // If the rect doesn't fit into a specific quadrant (overlaps multiple),
                // we must query all quadrants that it touches.
                // Simplified: query all subnodes if we can't determine a single one.
                // Or strictly check intersection.
                // For now, naive approach: if it doesn't fit one, retrieve from all.
                foreach (var node in nodes)
                {
                    // Optimization: check intersection with node bounds
                    if (node.bounds.IntersectsWith(pRect))
                    {
                        node.Retrieve(returnObjects, pRect);
                    }
                }
            }

            returnObjects.AddRange(objects);

            return returnObjects;
        }
    }
}