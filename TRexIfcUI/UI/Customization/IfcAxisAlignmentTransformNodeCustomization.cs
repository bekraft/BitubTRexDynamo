using Task;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class IfcAxisAlignmentTransformNodeCustomization : CancelableProgressingOptionNodeCustomization<IfcAxisAlignmentTransformNodeModel>
    {
        public IfcAxisAlignmentTransformNodeCustomization(): base(ProgressOnPortType.OutPorts, Log.LogReason.Changed)
        {
        }
    }

#pragma warning restore CS1591
}
