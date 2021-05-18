using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using Export;

using System.Linq;

namespace UI.Customization
{
#pragma warning disable CS1591
    public class SceneSaveNodeCustomization : CancelableOptionCommandCustomization<SceneSaveNodeModel>
    {
        public SceneSaveNodeCustomization() : base(ProgressOnPortType.InPorts)
        { }
    }
#pragma warning restore CS1591
}
