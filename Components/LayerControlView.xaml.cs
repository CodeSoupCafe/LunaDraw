using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;

namespace LunaDraw.Components
{
    public partial class LayerControlView : ContentView
    {
        public List<MaskingMode> MaskingModes { get; } = Enum.GetValues(typeof(MaskingMode)).Cast<MaskingMode>().ToList();

        public LayerControlView()
        {
            InitializeComponent();
        }

        private void OnCollapseClicked(object sender, EventArgs e)
        {
            ContentGrid.IsVisible = !ContentGrid.IsVisible;
            CollapseButton.Text = ContentGrid.IsVisible ? "▼" : "▶";
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
