using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using TRex.Store;
using System.Linq;

namespace TRex.UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class IfcSaveNodeCustomization : CancelableOptionCommandCustomization<IfcSaveStoreNodeModel>
    {
        public IfcSaveNodeCustomization() : base(ProgressOnPortType.InPorts)
        {            
        }

        public override void CustomizeView(IfcSaveStoreNodeModel model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);
            model.PortConnected += Model_PortConnected;
        }

        private void Model_PortConnected(PortModel portModel, ConnectorModel connector)
        {
            switch(portModel.PortType)
            {
                case PortType.Input:
                    var ifcModels = NodeModel.GetCachedInput<IfcModel>(portModel.Index, ModelEngineController);
                    // Autofix extension if unique
                    if (1 == ifcModels.Length)
                        NodeModel.SelectedOption = ifcModels.FirstOrDefault()?.FormatExtension;
                    break;
                case PortType.Output:
                    break;
            }
        }

        public override void Dispose()
        {
            NodeModel.PortConnected -= Model_PortConnected;
            base.Dispose();
        }
    }

#pragma warning restore CS1591
}
