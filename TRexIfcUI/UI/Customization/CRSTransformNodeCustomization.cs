using Dynamo.Controls;
using TRex.Export;

namespace TRex.UI.Customization
{
#pragma warning disable CS1591

    public class CRSTransformNodeCustomization : BaseNodeViewCustomization<CRSTransformNodeModel>
    {
        public override void CustomizeView(CRSTransformNodeModel model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);
            var control = new CRSTransformControl();
            nodeView.inputGrid.Children.Add(control);
            control.DataContext = model;
        }

        public override void Dispose()
        {            
        }
    }

#pragma warning restore CS1591
}
