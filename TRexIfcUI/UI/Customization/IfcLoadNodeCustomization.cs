using Store;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class IfcLoadNodeCustomization : CancelableProgressingNodeCustomization<IfcLoadStoreNodeModel>
    {
        public IfcLoadNodeCustomization() : base(ProgressOnPortType.OutPorts, Log.ActionType.Loaded)
        { 
        }
    }

    // Disable comment warning
#pragma warning restore CS1591

}