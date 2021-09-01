using Dynamo.Controls;

using TRex.Export;

namespace TRex.UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class SceneBuildSettingsCustomization : BaseNodeViewCustomization<SceneBuildSettingsNodeModel>
    {
        public override void CustomizeView(SceneBuildSettingsNodeModel model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);

            var control = new SceneBuildSettingsControl();
            nodeView.inputGrid.Children.Add(control);
            control.DataContext = model;
        }

        public override void Dispose()
        {            
        }
    }

#pragma warning restore CS1591
}

