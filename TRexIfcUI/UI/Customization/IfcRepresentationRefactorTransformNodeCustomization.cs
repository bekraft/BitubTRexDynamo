using TRex.Task;

namespace TRex.UI.Customization
{
#pragma warning disable CS1591

    public class IfcRepresentationRefactorTransformNodeCustomization : CancelableCommandCustomization<IfcRepresentationRefactorTransformNodeModel>
    {
        public IfcRepresentationRefactorTransformNodeCustomization() : base(ProgressOnPortType.OutPorts)
        {
        }
    }

#pragma warning restore CS1591
}
