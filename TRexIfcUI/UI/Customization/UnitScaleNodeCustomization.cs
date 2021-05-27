using Dynamo.Controls;

using TRex.Export;

namespace TRex.UI.Customization
{
#pragma warning disable CS1591

    public class UnitScaleNodeCustomization : BaseNodeViewCustomization<UnitScaleNodeModel>
    {
        public override void CustomizeView(UnitScaleNodeModel model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);
            var control = new UnitScaleControl();
            nodeView.inputGrid.Children.Add(control);
            control.DataContext = model;
        }

        public override void Dispose()
        {            
        }
    }

#pragma warning restore CS1591
}
