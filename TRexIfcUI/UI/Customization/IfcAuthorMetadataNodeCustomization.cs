using Dynamo.Controls;
using Dynamo.Wpf;

using Store;

namespace UI.Customization
{
#pragma warning disable CS1591

    public class IfcAuthorMetadataNodeCustomization : INodeViewCustomization<IfcAuthorMetadataNodeModel>
    {
        public void CustomizeView(IfcAuthorMetadataNodeModel model, NodeView nodeView)
        {
            var metaDataControl = new AuthoringMetaDataControl();
            nodeView.inputGrid.Children.Add(metaDataControl);
            metaDataControl.DataContext = model;
        }

        public void Dispose()
        {            
        }
    }

#pragma warning restore CS1591
}

