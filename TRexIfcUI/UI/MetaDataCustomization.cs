using Dynamo.Controls;
using Dynamo.Wpf;

namespace TRexIfc.UI
{
    public class MetaDataCustomization : INodeViewCustomization<IfcMetadataNodeModel>
    {
        public void CustomizeView(IfcMetadataNodeModel model, NodeView nodeView)
        {
            var metaDataControl = new MetaDataControl();
            nodeView.inputGrid.Children.Add(metaDataControl);
            metaDataControl.DataContext = model;
        }

        public void Dispose()
        {            
        }
    }
}
