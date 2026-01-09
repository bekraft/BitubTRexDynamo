using TRex.Task;

namespace TRex.UI.Customization;

public sealed class MapConversionTransformNodeCustomization : CancelableCommandCustomization<MapConversionTransformNodeModel>
{
    public MapConversionTransformNodeCustomization() : base(ProgressOnPortType.OutPorts)
    {
    }
}