using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using Export;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class SceneExporterNodeCustomization : CancelableProgressingOptionNodeCustomization<SceneExporterNodeModel>
    {
        public SceneExporterNodeCustomization(): base(ProgressOnPortType.InPorts, Log.ActionType.Saved)
        {
        }

        public override void CustomizeView(SceneExporterNodeModel model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);
        }
    }

#pragma warning restore CS1591
}
