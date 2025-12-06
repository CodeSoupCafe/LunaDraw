using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;

namespace LunaDraw.Components
{
    public partial class LayerControlView : ContentView
    {
        public static readonly BindableProperty IsLayerPanelExpandedProperty =
            BindableProperty.Create(nameof(IsLayerPanelExpanded), typeof(bool), typeof(LayerControlView), false, propertyChanged: OnIsLayerPanelExpandedChanged);

        public bool IsLayerPanelExpanded
        {
            get => (bool)GetValue(IsLayerPanelExpandedProperty);
            set => SetValue(IsLayerPanelExpandedProperty, value);
        }

        public List<MaskingMode> MaskingModes { get; } = Enum.GetValues<MaskingMode>().Cast<MaskingMode>().ToList();

        public LayerControlView()
        {
            InitializeComponent();
        }

        private static void OnIsLayerPanelExpandedChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (LayerControlView)bindable;
            control.ContentGrid.IsVisible = (bool)newValue;
            control.CollapseButton.Text = (bool)newValue ? "▼" : "▶";
        }

        private void OnCollapseClicked(object sender, EventArgs e)
        {
            IsLayerPanelExpanded = !IsLayerPanelExpanded;
        }

        private void OnDragStarting(object sender, DragStartingEventArgs e)
        {
            if (sender is Element element && element.BindingContext is Layer layer)
            {
                e.Data.Properties["SourceLayer"] = layer;
                // Ensure the dragged layer is selected
                if (this.BindingContext is MainViewModel viewModel)
                {
                    viewModel.CurrentLayer = layer;
                }
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private void OnDrop(object sender, DropEventArgs e)
        {
            if (e.Data.Properties.TryGetValue("SourceLayer", out var sourceObj) && sourceObj is Layer sourceLayer)
            {
                if (sender is Element element && element.BindingContext is Layer targetLayer)
                {
                    if (this.BindingContext is MainViewModel viewModel)
                    {
                         viewModel.ReorderLayer(sourceLayer, targetLayer);
                    }
                }
            }
        }
    }
}
