using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using TRex.Export;

namespace TRex.UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class SceneBuildNodeCustomization : CancelableCommandCustomization<SceneBuildNodeModel>
    {
        public SceneBuildNodeCustomization(): base(ProgressOnPortType.InPorts)
        {
        }
    }

#pragma warning restore CS1591
}
