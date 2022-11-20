using TRex.Task;
using TRex.UI.Model;

using Bitub.Xbim.Ifc.Transform;

namespace TRex.UI.Customization
{
#pragma warning disable CS1591

    public class IfcRepresentationRefactorTransformNodeCustomization 
        : CancelableOptionCommandCustomization<IfcRepresentationRefactorTransformNodeModel, IfcProductRefactorOption>
    {
        public IfcRepresentationRefactorTransformNodeCustomization() : base(ProgressOnPortType.OutPorts)
        {
        }
    }

#pragma warning restore CS1591
}
