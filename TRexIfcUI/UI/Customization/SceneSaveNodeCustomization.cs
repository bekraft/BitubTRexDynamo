using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using TRex.Export;

using System.Linq;

namespace TRex.UI.Customization
{
#pragma warning disable CS1591
    public class SceneSaveNodeCustomization : CancelableOptionCommandCustomization<SceneSaveNodeModel>
    {
        public SceneSaveNodeCustomization() : base(ProgressOnPortType.InPorts)
        { }
    }
#pragma warning restore CS1591
}
