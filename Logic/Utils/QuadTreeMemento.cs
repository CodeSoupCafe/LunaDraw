/* 
 *  Copyright (c) 2025 CodeSoupCafe LLC
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 *  
 */

using SkiaSharp;

namespace LunaDraw.Logic.Utils;

public class QuadTreeMemento<T>(int level, SKRect bounds, Func<T, SKRect> getBounds) where T : class
{
  private readonly int maxObjects = 10;
  private readonly int maxLevels = 5;

  private readonly int level = level;
  private readonly List<T> objects = [];
  private readonly SKRect bounds = bounds;
  private readonly Func<T, SKRect> getBounds = getBounds;
  private QuadTreeMemento<T>[]? nodes;

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

    nodes = new QuadTreeMemento<T>[4];
    nodes[0] = new QuadTreeMemento<T>(level + 1, new SKRect(x + subWidth, y, x + subWidth + subWidth, y + subHeight), getBounds);
    nodes[1] = new QuadTreeMemento<T>(level + 1, new SKRect(x, y, x + subWidth, y + subHeight), getBounds);
    nodes[2] = new QuadTreeMemento<T>(level + 1, new SKRect(x, y + subHeight, x + subWidth, y + subHeight + subHeight), getBounds);
    nodes[3] = new QuadTreeMemento<T>(level + 1, new SKRect(x + subWidth, y + subHeight, x + subWidth + subWidth, y + subHeight + subHeight), getBounds);
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