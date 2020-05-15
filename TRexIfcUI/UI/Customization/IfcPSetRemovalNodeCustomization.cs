using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using Store;
using System.Linq;
using Task;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class IfcPSetRemovalNodeCustomization : CancelableProgressingNodeCustomization<IfcPSetRemovalTransformNodeModel>
    {
        public IfcPSetRemovalNodeCustomization() : base(ProgressOnPortType.OutPorts, Log.ActionType.Changed)
        {
        }
    }

#pragma warning restore CS1591
}
