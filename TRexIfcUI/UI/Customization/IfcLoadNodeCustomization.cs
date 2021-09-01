using TRex.Store;

namespace TRex.UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class IfcLoadNodeCustomization : CancelableCommandCustomization<IfcLoadStoreNodeModel>
    {
        public IfcLoadNodeCustomization() : base(ProgressOnPortType.OutPorts)
        { 
        }
    }

    // Disable comment warning
#pragma warning restore CS1591

}