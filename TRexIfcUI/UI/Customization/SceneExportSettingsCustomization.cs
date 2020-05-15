using System;
using System.Linq;
using System.Windows.Controls;

using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using Dynamo.Controls;

using Export;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class SceneExportSettingsCustomization : BaseNodeViewCustomization<SceneExportSettingsNodeModel>
    {
        #region Internals

        private SceneExportSettingsControl _control;

        #endregion

        public override void CustomizeView(SceneExportSettingsNodeModel model, NodeView nodeView)

        {
            base.CustomizeView(model, nodeView);

            _control = new SceneExportSettingsControl();
            nodeView.inputGrid.Children.Add(_control);
            _control.DataContext = model;

            Init(model, _control);

            model.PortConnected += Model_PortConnected;
            model.Modified += Model_Modified;
            
            _control.SceneContextListBox.SelectionChanged += SceneContextListBox_SelectionChanged;
        }

        private void Init(SceneExportSettingsNodeModel model, SceneExportSettingsControl view)
        {
            foreach (string c in model.SelectedGraphicalContext)
                view.SceneContextListBox.SelectedItems.Add(c);
        }

        private void SceneContextListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get all selected context identifiers
            var selectedContexts = (sender as ListBox).SelectedItems.Cast<string>().ToArray();
            // And synchronize
            ScheduleAsync(() => NodeModel.SynchronizeSelectedSceneContexts(selectedContexts, true));
        }

        private void Model_Modified(NodeModel obj)
        {
            ScheduleAsync(() => {
                var providedContexts = NodeModel.GetCachedInput<string>(2, ModelEngineController);
                NodeModel.MergeProvidedSceneContext(providedContexts);
            });            
        }

        private void Model_PortConnected(PortModel pm, ConnectorModel cm)
        {            
            ScheduleAsync(() => {
                var providedContexts = NodeModel.GetCachedInput<string>(2, ModelEngineController);
                NodeModel.MergeProvidedSceneContext(providedContexts);
            });
        }

        public override void Dispose()
        {            
            NodeModel.PortConnected -= Model_PortConnected;
            NodeModel.Modified -= Model_Modified;
            _control.SceneContextListBox.SelectionChanged -= SceneContextListBox_SelectionChanged;
        }
    }

#pragma warning restore CS1591
}

