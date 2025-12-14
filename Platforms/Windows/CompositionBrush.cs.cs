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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

// Alias namespaces to avoid ambiguity and ensure correct types are used
using WinComp = Windows.UI.Composition;
using WinUIComp = Microsoft.UI.Composition;

namespace LunaDraw.WinUI;

public abstract class CompositionBrushBackdrop : SystemBackdrop
{
  private WinComp.CompositionBrush? brush;
  private WinComp.Compositor? compositor;

  protected abstract WinComp.CompositionBrush CreateBrush(WinComp.Compositor compositor);

  protected override void OnTargetConnected(WinUIComp.ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
  {
    base.OnTargetConnected(connectedTarget, xamlRoot);

    compositor = new WinComp.Compositor();

    brush = CreateBrush(compositor);

    connectedTarget.SystemBackdrop = brush;
  }

  protected override void OnTargetDisconnected(WinUIComp.ICompositionSupportsSystemBackdrop disconnectedTarget)
  {
    base.OnTargetDisconnected(disconnectedTarget);

    if (brush != null)
    {
      brush.Dispose();
      brush = null;
    }

    if (compositor != null)
    {
      compositor.Dispose();
      compositor = null;
    }
  }
}
