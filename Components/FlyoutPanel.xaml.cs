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

using System.Windows.Input;

namespace LunaDraw.Components;

public partial class FlyoutPanel : ContentView
{
  private bool isOpen;
  private View? targetElement;

  public static readonly BindableProperty FlyoutContentProperty =
      BindableProperty.Create(
          nameof(FlyoutContent),
          typeof(View),
          typeof(FlyoutPanel),
          default(View),
          propertyChanged: OnFlyoutContentChanged);

  public static readonly BindableProperty IsOpenProperty =
      BindableProperty.Create(
          nameof(IsOpen),
          typeof(bool),
          typeof(FlyoutPanel),
          default(bool),
          propertyChanged: OnIsOpenChanged);

  public static readonly BindableProperty TargetElementProperty =
      BindableProperty.Create(
          nameof(TargetElement),
          typeof(View),
          typeof(FlyoutPanel),
          default(View),
          propertyChanged: OnTargetElementChanged);

  // Optional: name of a child inside TargetElement to use as the anchor (e.g., a specific button inside the toolbar)
  public static readonly BindableProperty AnchorNameProperty =
      BindableProperty.Create(
          nameof(AnchorName),
          typeof(string),
          typeof(FlyoutPanel),
          default(string));

  public FlyoutPanel()
  {
    InitializeComponent();

    // Initialize to hidden state
    this.Opacity = 0;
    this.Scale = 0.9;
    AbsoluteLayout.SetLayoutBounds(this, new Rect(-1000, -1000, -1, -1));
  }

  public View FlyoutContent
  {
    get => (View)GetValue(FlyoutContentProperty);
    set => SetValue(FlyoutContentProperty, value);
  }

  public bool IsOpen
  {
    get => (bool)GetValue(IsOpenProperty);
    set => SetValue(IsOpenProperty, value);
  }

  public View TargetElement
  {
    get => (View)GetValue(TargetElementProperty);
    set => SetValue(TargetElementProperty, value);
  }

  public string AnchorName
  {
    get => (string)GetValue(AnchorNameProperty);
    set => SetValue(AnchorNameProperty, value);
  }

  public ICommand? CloseCommand { get; private set; }

  private static void OnFlyoutContentChanged(BindableObject bindable, object oldValue, object newValue)
  {
    var panel = (FlyoutPanel)bindable;
    // Content is handled by the ContentPresenter in XAML
  }

  private static void OnIsOpenChanged(BindableObject bindable, object oldValue, object newValue)
  {
    var panel = (FlyoutPanel)bindable;
    panel.isOpen = (bool)newValue;
    panel.UpdateVisibility();
  }

  private static void OnTargetElementChanged(BindableObject bindable, object oldValue, object newValue)
  {
    var panel = (FlyoutPanel)bindable;
    panel.targetElement = (View)newValue;
  }

  private async void UpdateVisibility()
  {
    if (isOpen)
    {
      await ShowFlyout();
    }
    else
    {
      await HideFlyout();
    }
  }

  private async Task ShowFlyout()
  {
    if (targetElement != null)
    {
      await PositionFlyout();
    }

    // Animate in - run fade and scale simultaneously
    var fadeTask = this.FadeToAsync(1, 200, Easing.CubicOut);
    var scaleTask = this.ScaleToAsync(1, 200, Easing.CubicOut);

    await Task.WhenAll(fadeTask, scaleTask);
  }

  private async Task HideFlyout()
  {
    // Animate out - run fade and scale simultaneously
    var fadeTask = this.FadeToAsync(0, 150, Easing.CubicIn);
    var scaleTask = this.ScaleToAsync(0.9, 150, Easing.CubicIn);

    await Task.WhenAll(fadeTask, scaleTask);

    // Move off-screen when hidden
    AbsoluteLayout.SetLayoutBounds(this, new Rect(-1000, -1000, -1, -1));
  }

  public static Rect GetCoordinatesWithinPage(VisualElement element)
  {
    double x = 0;
    double y = 0;

    VisualElement? currentElement = element;

    // Traverse up the visual tree until a Page or null is encountered
    while (currentElement != null && !(currentElement is Page))
    {
      x += currentElement.X;
      y += currentElement.Y;
      currentElement = currentElement.Parent as VisualElement;

      if (currentElement is ScrollView scrollView)
      {
        if (scrollView.Orientation == ScrollOrientation.Both || scrollView.Orientation == ScrollOrientation.Horizontal)
        {
          x -= scrollView.ScrollX;
        }
        if (scrollView.Orientation == ScrollOrientation.Both || scrollView.Orientation == ScrollOrientation.Vertical)
        {
          y -= scrollView.ScrollY;
        }
      }
    }

    // If the element is within a Page, add the Page's X and Y
    if (currentElement is Page page)
    {
      x += page.X;
      y += page.Y;
    }

    return new Rect(x, y, element.Width, element.Height);
  }

  private async Task PositionFlyout()
  {
    if (targetElement == null) return;

    // Find the actual element to anchor to, if an AnchorName is provided
    View anchorElement = targetElement;
    if (!string.IsNullOrEmpty(AnchorName))
    {
      anchorElement = anchorElement.FindByName(AnchorName) as View ?? targetElement;
    }

    // Compute target bounds relative to the page
    var targetBounds = GetCoordinatesWithinPage(anchorElement); // Changed to use anchorElement
    if (targetBounds == Rect.Zero) return;

    // Position to the right of the target element with a small gap
    var x = targetBounds.Right + 10;
    var y = targetBounds.Top;

    // Set initial bounds using -1 to indicate AutoSize for width/height
    AbsoluteLayout.SetLayoutBounds(this, new Rect(x, y, -1, -1));

    // Wait one layout cycle so the FlyoutContainer can size itself
    await Task.Yield();

    var flyoutBounds = this.Bounds;

    // Get parent page dimensions (assume top-level layout fills the page)
    if (!(this.Parent is VisualElement parentPage)) return;
    var screenWidth = parentPage.Width;
    var screenHeight = parentPage.Height;
    var margin = 10.0;

    double finalX = x;
    double finalY = y;
    double finalWidth = -1;
    double finalHeight = -1;

    // --- Horizontal Positioning Strategy ---

    // 1. Try positioning to the right of the target (preferred)
    if (flyoutBounds.Right > screenWidth - margin)
    {
      // It overflows right. Try positioning to the left of the target.
      double leftX = targetBounds.Left - flyoutBounds.Width - 10;
      if (leftX >= margin)
      {
        finalX = leftX;
      }
      else
      {
        // Neither side fits perfectly.
        // Position at the left-most valid position or right-most valid position?
        // Let's constrain to the screen width.
        finalX = Math.Max(margin, Math.Min(x, screenWidth - flyoutBounds.Width - margin));

        // If the flyout is wider than the screen (minus margins), constrain width.
        if (flyoutBounds.Width > screenWidth - 2 * margin)
        {
          finalX = margin;
          finalWidth = screenWidth - 2 * margin;
        }
      }
    }

    // --- Vertical Positioning Strategy ---

    // If the flyout overflows the bottom edge
    if (flyoutBounds.Bottom > screenHeight - margin)
    {
      // Try moving it up
      double diff = flyoutBounds.Bottom - (screenHeight - margin);
      double newY = y - diff;

      if (newY >= margin)
      {
        finalY = newY;
      }
      else
      {
        // Moving up hits the top edge. Constrain Height.
        finalY = margin;
        finalHeight = screenHeight - 2 * margin;
      }
    }

    // Re-apply the adjusted bounds
    // Note: AbsoluteLayout in MAUI handles -1 as AutoSize.
    // If we set a specific width/height, it will respect it.
    AbsoluteLayout.SetLayoutBounds(this, new Rect(finalX, finalY, finalWidth, finalHeight));
  }
}