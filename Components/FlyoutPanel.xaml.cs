using System.Windows.Input;

namespace LunaDraw.Components
{
  public partial class FlyoutPanel : ContentView
  {
    private bool _isOpen;
    private View? _targetElement;

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
      FlyoutContainer.Scale = 0.9;
      AbsoluteLayout.SetLayoutBounds(FlyoutContainer, new Rect(-1000, -1000, -1, -1));
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
      panel._isOpen = (bool)newValue;
      panel.UpdateVisibility();
    }

    private static void OnTargetElementChanged(BindableObject bindable, object oldValue, object newValue)
    {
      var panel = (FlyoutPanel)bindable;
      panel._targetElement = (View)newValue;
    }

    private async void UpdateVisibility()
    {
      if (_isOpen)
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
      if (_targetElement != null)
      {
        await PositionFlyout();
      }

      // Animate in - run fade and scale simultaneously
      var fadeTask = FlyoutRoot.FadeToAsync(1, 200, Easing.CubicOut);
      var scaleTask = FlyoutRoot.ScaleToAsync(1, 200, Easing.CubicOut);

      await Task.WhenAll(fadeTask, scaleTask);
    }

    private async Task HideFlyout()
    {
      // Animate out - run fade and scale simultaneously
      var fadeTask = FlyoutRoot.FadeToAsync(0, 150, Easing.CubicIn);
      var scaleTask = FlyoutRoot.ScaleToAsync(0.9, 150, Easing.CubicIn);

      await Task.WhenAll(fadeTask, scaleTask);

      // Move off-screen when hidden
      AbsoluteLayout.SetLayoutBounds(FlyoutRoot, new Rect(-1000, -1000, -1, -1));
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
      if (_targetElement == null) return;

      // Find the actual element to anchor to, if an AnchorName is provided
      View anchorElement = _targetElement;
      if (!string.IsNullOrEmpty(AnchorName))
      {
        anchorElement = anchorElement.FindByName(AnchorName) as View ?? _targetElement;
      }

      // Compute target bounds relative to the page
      var targetBounds = GetCoordinatesWithinPage(anchorElement); // Changed to use anchorElement
      if (targetBounds == Rect.Zero) return;

      // Position to the right of the target element with a small gap
      var x = targetBounds.Right + 10;
      var y = targetBounds.Top;

      // Set initial bounds using -1 to indicate AutoSize for width/height
      AbsoluteLayout.SetLayoutBounds(FlyoutRoot, new Rect(x, y, -1, -1));

      // Wait one layout cycle so the FlyoutContainer can size itself
      await Task.Yield();

      var flyoutBounds = FlyoutRoot.Bounds;

      // Get parent page dimensions (assume top-level layout fills the page)
      if (!(this.Parent is VisualElement parentPage)) return;
      var screenWidth = parentPage.Width;
      var screenHeight = parentPage.Height;

      // If the flyout would overflow the right edge, move it to the left of the target
      if (flyoutBounds.Right > screenWidth)
      {
        x = targetBounds.Left - flyoutBounds.Width - 10;
      }

      // If the flyout would overflow the bottom edge, move it up to fit
      if (flyoutBounds.Bottom > screenHeight)
      {
        y = Math.Max(0, screenHeight - flyoutBounds.Height);
      }

      // Re-apply the adjusted bounds (still use -1 for AutoSize)
      AbsoluteLayout.SetLayoutBounds(FlyoutRoot, new Rect(x, y, -1, -1));
    }
  }
}
