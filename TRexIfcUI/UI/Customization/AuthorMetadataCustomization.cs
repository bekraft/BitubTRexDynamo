using Dynamo.Controls;
using Dynamo.Wpf;

using Store;

namespace UI
{
#pragma warning disable CS1591

    public class AuthorMetadataCustomization : INodeViewCustomization<AuthorDataNodeModel>
    {
        public void CustomizeView(AuthorDataNodeModel model, NodeView nodeView)
        {
            var metaDataControl = new MetaDataControl();
            nodeView.inputGrid.Children.Add(metaDataControl);
            metaDataControl.DataContext = model;
        }

        public void Dispose()
        {            
        }
    }

#pragma warning restore CS1591
}

