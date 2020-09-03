using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using Export;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class SceneExporterNodeCustomization : CancelableOptionCommandCustomization<SceneExporterNodeModel>
    {
        public SceneExporterNodeCustomization(): base(ProgressOnPortType.InPorts, Log.LogReason.Saved)
        {
        }
    }

#pragma warning restore CS1591
}
