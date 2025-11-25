using System.Windows.Input;

namespace LunaDraw.Components
{
  public partial class FlyoutPanel : ContentView
  {
    private bool _isOpen;
    private View _targetElement;

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

    public ICommand CloseCommand { get; private set; }

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
        // FlyoutContainer.InputTransparent = false; // Enable input when visible
        await ShowFlyout();
      }
      else
      {
        // FlyoutContainer.InputTransparent = true; // Disable input when hidden
        await HideFlyout();
      }
    }

    private async Task ShowFlyout()
    {
      if (_targetElement != null)
      {
        await PositionFlyout();
      }

      // Animate in
      await FlyoutContainer.FadeToAsync(1, 200, Easing.CubicOut);
      await FlyoutContainer.ScaleToAsync(1, 200, Easing.CubicOut);
    }

    private async Task HideFlyout()
    {
      // Animate out
      await FlyoutContainer.FadeToAsync(0, 150, Easing.CubicIn);
      await FlyoutContainer.ScaleToAsync(0.9, 150, Easing.CubicIn);

      // Move off-screen when hidden
      AbsoluteLayout.SetLayoutBounds(FlyoutContainer, new Rect(-1000, -1000, -1, -1));
    }

    private async Task PositionFlyout()
    {
      if (_targetElement == null) return;

      // Fallback: compute absolute position by walking the visual tree and summing bounds.
      // Fallback: compute absolute position by walking the visual tree and summing bounds.
      Rect GetAbsoluteBounds(View v)
      {
        var r = v.Bounds;
        Element parent = v.Parent;
        while (parent is VisualElement ve)
        {
          r = new Rect(r.X + ve.X, r.Y + ve.Y, r.Width, r.Height);
          parent = ve.Parent;
        }
        return r;
      }

      // Find the actual element to anchor to, if an AnchorName is provided
      View anchorElement = _targetElement;
      if (!string.IsNullOrEmpty(AnchorName) && _targetElement is Layout targetLayout)
      {
        anchorElement = targetLayout.FindByName(AnchorName) as View ?? _targetElement;
      }

      // Compute target bounds relative to the page
      var targetBounds = GetAbsoluteBounds(anchorElement); // Changed to use anchorElement
      if (targetBounds == Rect.Zero) return;

      // Position to the right of the target element with a small gap
      var x = targetBounds.Right + 10;
      var y = targetBounds.Top;

      // Set initial bounds using -1 to indicate AutoSize for width/height
      AbsoluteLayout.SetLayoutBounds(FlyoutContainer, new Rect(x, y, -1, -1));

      // Wait one layout cycle so the FlyoutContainer can size itself
      await Task.Yield();

      var flyoutBounds = FlyoutContainer.Bounds;

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
      AbsoluteLayout.SetLayoutBounds(FlyoutContainer, new Rect(x, y, -1, -1));
    }
  }
}
