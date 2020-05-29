using System;
using System.Linq;
using System.Windows.Controls;

using Dynamo.Controls;
using Internal;

using Task;

namespace UI.Customization
{
#pragma warning disable CS1591

    public class SelectiveItemsNodeCustomization : BaseNodeViewCustomization<SelectableItemListNodeModel>
    {
        #region Internals

        private SelectableListControl _control;

        #endregion

        public override void CustomizeView(SelectableItemListNodeModel model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);

            _control = new SelectableListControl();
            nodeView.inputGrid.Children.Add(_control);
            _control.DataContext = model;

            InitSelection(model.Selected.ToArray());

            model.PortDisconnected += Model_PortDisconnected;
            model.PortConnected += Model_PortConnected;
            model.Modified += Model_Modified;

            _control.SelectionListBox.SelectionChanged += SelectionListBox_SelectionChanged;
        }

        private void InitSelection(AstReference[] selection)
        {
            UpdateAvailables();
            DispatchUI(() =>
            {   
                foreach (var s in AstValue<object>.Resolve(selection, NodeModel.Items))
                    _control.SelectionListBox.SelectedItems.Add(s);
            });
        }

        private void UpdateAvailables()
        {
            var items = NodeModel.GetCachedAstInput<object>(SelectableItemListNodeModel.ID_ITEMS_IN, ModelEngineController);
            NodeModel.SetItems(items);
        }

        private void SelectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get all selected context identifiers
            var selected = (sender as ListBox).SelectedItems.Cast<AstReference>().ToArray();
            // And synchronize
            ScheduleAsync(() => NodeModel.SynchronizeSelected(selected, true));
        }

        private void Model_Modified(Dynamo.Graph.Nodes.NodeModel obj)
        {
            ScheduleAsync(UpdateAvailables);
        }

        private void Model_PortConnected(Dynamo.Graph.Nodes.PortModel arg1, Dynamo.Graph.Connectors.ConnectorModel arg2)
        {
            ScheduleAsync(UpdateAvailables);
        }

        private void Model_PortDisconnected(Dynamo.Graph.Nodes.PortModel obj)
        {
            ScheduleAsync(UpdateAvailables);
        }

        public override void Dispose()
        {
            if (null == _control)
                throw new ObjectDisposedException(nameof(SelectiveItemsNodeCustomization));

            _control.SelectionListBox.SelectionChanged -= SelectionListBox_SelectionChanged;
            NodeModel.PortDisconnected -= Model_PortDisconnected;
            NodeModel.PortConnected -= Model_PortConnected;
            NodeModel.Modified -= Model_Modified;

            _control = null;
        }
    }

#pragma warning restore CS1591

}
