using TRex.Export;

namespace TRex.UI.Customization;

public class SceneExportNodeCustomization : CancelableOptionCommandCustomization<SceneExportNodeModel, Format>
{
    public SceneExportNodeCustomization() : base(ProgressOnPortType.OutPorts)
    { }
}