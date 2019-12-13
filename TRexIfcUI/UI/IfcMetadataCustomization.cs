﻿using Dynamo.Controls;
using Dynamo.Wpf;

// Disable comment warning
#pragma warning disable CS1591

namespace TRexIfc.UI
{
    public class IfcMetadataCustomization : INodeViewCustomization<IfcMetadataNodeModel>
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

#pragma warning restore CS1591

