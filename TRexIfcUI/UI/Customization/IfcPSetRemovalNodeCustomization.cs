using Task;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class IfcPSetRemovalNodeCustomization : CancelableProgressingNodeCustomization<IfcPSetRemovalTransformNodeModel>
    {
        public IfcPSetRemovalNodeCustomization() : base(ProgressOnPortType.OutPorts, Log.LogReason.Changed)
        {
        }
    }

#pragma warning restore CS1591
}
