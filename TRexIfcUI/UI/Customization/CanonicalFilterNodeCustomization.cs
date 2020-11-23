using Data;
using Dynamo.Controls;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class CanonicalFilterNodeCustomization : BaseNodeViewCustomization<CanonicalFilterNodeModel>
    {
        public override void CustomizeView(CanonicalFilterNodeModel model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);
            var filterMatchControl = new FilterMatchTypeControl();
            nodeView.inputGrid.Children.Add(filterMatchControl);
            filterMatchControl.DataContext = model;
        }

        public override void Dispose()
        {            
        }
    }

    // Disable comment warning
#pragma warning restore CS1591

}
