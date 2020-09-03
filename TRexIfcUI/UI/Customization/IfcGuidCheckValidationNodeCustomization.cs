using Validation;

namespace UI.Customization
{

    // Disable comment warning
#pragma warning disable CS1591

    public class IfcGuidCheckValidationNodeCustomization : CancelableCommandCustomization<IfcGuidCheckValidationNodeModel>
    {
        public IfcGuidCheckValidationNodeCustomization(): base(ProgressOnPortType.OutPorts, Log.LogReason.Checked)
        {
        }
    }

#pragma warning restore CS1591

}
