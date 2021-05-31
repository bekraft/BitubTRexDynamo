using TRex.Task;

namespace TRex.UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class IfcAxisAlignmentTransformNodeCustomization : CancelableOptionCommandCustomization<IfcAxisAlignmentTransformNodeModel, string>
    {
        public IfcAxisAlignmentTransformNodeCustomization(): base(ProgressOnPortType.OutPorts)
        {
        }
    }

#pragma warning restore CS1591
}
