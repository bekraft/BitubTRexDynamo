using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using System.Linq;

using TRex.Export;

namespace TRex.UI.Customization
{
#pragma warning disable CS1591
    public class SceneExportNodeCustomization : CancelableOptionCommandCustomization<SceneExportNodeModel>
    {
        public SceneExportNodeCustomization() : base(ProgressOnPortType.InPorts)
        { }
    }
#pragma warning restore CS1591
}
