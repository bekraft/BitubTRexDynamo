using System.Linq;

using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using Dynamo.Controls;
using Dynamo.Engine;
using Dynamo.Wpf;
using Dynamo.Scheduler;

using Export;
using Store;

using System;
using System.Windows.Controls;
using Dynamo.ViewModels;
using System.Collections.ObjectModel;
using Microsoft.Practices.Prism;

namespace UI
{
    // Disable comment warning
#pragma warning disable CS1591

    public class SceneExportSettingsCustomization : BaseNodeViewCustomization<SceneExportSettingsNodeModel>
    {
        public override void CustomizeView(SceneExportSettingsNodeModel model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);

            var sceneExportSettings = new SceneExportSettingsControl();
            nodeView.inputGrid.Children.Add(sceneExportSettings);
            sceneExportSettings.DataContext = model;

            Init(model, sceneExportSettings);

            model.PortConnected += Model_PortConnected;
            model.Modified += Model_Modified;

            
            sceneExportSettings.SceneContextListBox.SelectionChanged += SceneContextListBox_SelectionChanged;
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
            ScheduleAsync(() => Model.SynchronizeSelectedSceneContexts(selectedContexts, true));
        }

        private void Model_Modified(NodeModel obj)
        {
            ScheduleAsync(() => {
                var providedContexts = Model.GetProvidedContextInput(ModelEngineController);
                Model.MergeProvidedSceneContext(providedContexts);
            });            
        }

        private void Model_PortConnected(PortModel pm, ConnectorModel cm)
        {            
            ScheduleAsync(() => {
                var providedContexts = Model.GetProvidedContextInput(ModelEngineController);
                Model.MergeProvidedSceneContext(providedContexts);
            });
        }

        public override void Dispose()
        {
        }
    }

#pragma warning restore CS1591
}

