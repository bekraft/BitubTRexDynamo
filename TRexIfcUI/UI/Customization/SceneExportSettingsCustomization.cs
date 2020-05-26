using Dynamo.Controls;

using Export;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class SceneExportSettingsCustomization : BaseNodeViewCustomization<SceneExportSettingsNodeModel>
    {
        public override void CustomizeView(SceneExportSettingsNodeModel model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);

            var control = new SceneExportSettingsControl();
            nodeView.inputGrid.Children.Add(control);
            control.DataContext = model;
        }

        public override void Dispose()
        {            
        }
    }

#pragma warning restore CS1591
}

