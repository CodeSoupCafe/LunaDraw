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

using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Models;

public class NavigationModel : ReactiveObject
{
  private SKMatrix viewMatrix = SKMatrix.CreateIdentity();

  // Single source of truth - this is what gets applied to the canvas
  public SKMatrix ViewMatrix
  {
    get => viewMatrix;
    set => this.RaiseAndSetIfChanged(ref viewMatrix, value);
  }

  private int canvasWidth;
  public int CanvasWidth
  {
    get => canvasWidth;
    set => this.RaiseAndSetIfChanged(ref canvasWidth, value);
  }

  private int canvasHeight;
  public int CanvasHeight
  {
    get => canvasHeight;
    set => this.RaiseAndSetIfChanged(ref canvasHeight, value);
  }

  public void Reset()
  {
    ViewMatrix = SKMatrix.CreateIdentity();
  }
}