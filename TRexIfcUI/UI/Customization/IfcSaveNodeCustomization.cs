using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using Store;
using System.Linq;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class IfcSaveNodeCustomization : CancelableProgressingOptionNodeCustomization<IfcSaveStoreNodeModel>
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
            var ifcModels = NodeModel.GetCachedInput<IfcModel>(portModel.Index, ModelEngineController);
            NodeModel.SelectedOption = ifcModels.FirstOrDefault()?.Store.FormatExtension;
        }

        public override void Dispose()
        {
        }
    }

#pragma warning restore CS1591
}
