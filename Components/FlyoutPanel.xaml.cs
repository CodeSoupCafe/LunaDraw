using Microsoft.Maui.Controls;
using System.Windows.Input;

namespace LunaDraw.Components
{
    public partial class FlyoutPanel : ContentView
    {
        private bool _isVisible;
        private View _targetElement;

        public static readonly BindableProperty FlyoutContentProperty =
            BindableProperty.Create(
                nameof(FlyoutContent),
                typeof(View),
                typeof(FlyoutPanel),
                default(View),
                propertyChanged: OnFlyoutContentChanged);

        public static readonly BindableProperty IsVisibleProperty =
            BindableProperty.Create(
                nameof(IsVisible),
                typeof(bool),
                typeof(FlyoutPanel),
                default(bool),
                propertyChanged: OnIsVisibleChanged);

        public static readonly BindableProperty TargetElementProperty =
            BindableProperty.Create(
                nameof(TargetElement),
                typeof(View),
                typeof(FlyoutPanel),
                default(View),
                propertyChanged: OnTargetElementChanged);

        public FlyoutPanel()
        {
            InitializeComponent();
        }


        public View FlyoutContent
        {
            get => (View)GetValue(FlyoutContentProperty);
            set => SetValue(FlyoutContentProperty, value);
        }

        public bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        public View TargetElement
        {
            get => (View)GetValue(TargetElementProperty);
            set => SetValue(TargetElementProperty, value);
        }

        public ICommand CloseCommand { get; }

        private static void OnFlyoutContentChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var panel = (FlyoutPanel)bindable;
            // Content is handled by the ContentPresenter in XAML
        }

        private static void OnIsVisibleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var panel = (FlyoutPanel)bindable;
            panel._isVisible = (bool)newValue;
            panel.UpdateVisibility();
        }

        private static void OnTargetElementChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var panel = (FlyoutPanel)bindable;
            panel._targetElement = (View)newValue;
        }

        private async void UpdateVisibility()
        {
            if (_isVisible)
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

            // Animate in
            var fadeInTask = FlyoutContainer.FadeTo(1, 200, Easing.CubicOut);
            var scaleInTask = FlyoutContainer.ScaleTo(1, 200, Easing.CubicOut);
            await Task.WhenAll(fadeInTask, scaleInTask);
        }

        private async Task HideFlyout()
        {
            // Animate out
            var fadeOutTask = FlyoutContainer.FadeTo(0, 150, Easing.CubicIn);
            var scaleOutTask = FlyoutContainer.ScaleTo(0.9, 150, Easing.CubicIn);
            await Task.WhenAll(fadeOutTask, scaleOutTask);

            // Move off-screen when hidden
            AbsoluteLayout.SetLayoutBounds(FlyoutContainer, new Rect(-1000, -1000, -1, -1));
        }

        private async Task PositionFlyout()
        {
            if (_targetElement == null) return;

            // Get the target element's position relative to the parent
            var parent = (VisualElement)this.Parent;
            if (parent == null) return;

            // Get target element's bounds in parent coordinates
            var targetBounds = _targetElement.Bounds;

            // Calculate position (to the right of the target element)
            var x = targetBounds.Right + 10; // 10px gap
            var y = targetBounds.Top;

            // Get screen dimensions
            var screenWidth = parent.Width;
            var screenHeight = parent.Height;

            // Get flyout dimensions (measure if needed)
            var flyoutWidth = FlyoutContainer.WidthRequest > 0 ? FlyoutContainer.WidthRequest : 250;
            var flyoutHeight = FlyoutContainer.HeightRequest > 0 ? FlyoutContainer.HeightRequest : 300;

            // Adjust for screen boundaries
            if (x + flyoutWidth > screenWidth)
            {
                // Position to the left of the target if it would go off screen
                x = targetBounds.Left - flyoutWidth - 10;
            }

            if (y + flyoutHeight > screenHeight)
            {
                // Adjust to fit on screen
                y = Math.Max(0, screenHeight - flyoutHeight);
            }

            // Set the position using AbsoluteLayout
            AbsoluteLayout.SetLayoutBounds(FlyoutContainer, new Rect(x, y, flyoutWidth, flyoutHeight));
        }
    }
}
