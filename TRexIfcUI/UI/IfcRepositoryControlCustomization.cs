using Dynamo.Wpf;
using Dynamo.Controls;
using TRexIfc;

namespace TRexIfc.UI
{
    public class IfcRepositoryControlCustomization : INodeViewCustomization<IfcRepositoryNodeModel>
    {
        public void CustomizeView(IfcRepositoryNodeModel model, NodeView nodeView)
        {
            var cancelableControl = new CancelableCommandControl();
            nodeView.inputGrid.Children.Add(cancelableControl);
            cancelableControl.DataContext = model;
        }

        public void Dispose()
        {            
        }
    }
}
