using TRex.Task;

namespace TRex.UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class IfcPSetRemovalNodeCustomization : CancelableCommandCustomization<IfcPSetRemovalTransformNodeModel>
    {
        public IfcPSetRemovalNodeCustomization() : base(ProgressOnPortType.OutPorts)
        {
        }
    }

#pragma warning restore CS1591
}
