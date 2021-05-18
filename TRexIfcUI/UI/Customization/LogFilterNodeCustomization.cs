using Dynamo.Controls;
using Dynamo.Wpf;

using TRex.Log;

namespace TRex.UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class LogFilterNodeCustomization : BaseNodeViewCustomization<LogFilterNodeModel>
    {
        public override void CustomizeView(LogFilterNodeModel model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);
            var control = new LogFilterControl();
            nodeView.inputGrid.Children.Add(control);
            control.DataContext = model;
        }

        public override void Dispose()
        {            
        }
    }

#pragma warning restore CS1591
}
