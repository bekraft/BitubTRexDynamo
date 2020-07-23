using System;
using System.Linq;
using System.Windows.Controls;

using Dynamo.Controls;
using Internal;

using Task;

namespace UI.Customization
{
#pragma warning disable CS1591

    public class SelectableItemsNodeCustomization : BaseNodeViewCustomization<SelectableItemListNodeModel>
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

            UpdateItems();

            model.PortDisconnected += Model_PortDisconnected;
            model.PortConnected += Model_PortConnected;

            _control.SelectionListBox.SelectionChanged += SelectionListBox_SelectionChanged;
        }

        protected override void OnCachedValueChange(object sender)
        {
            UpdateItems();
        }

        private void UpdateItems()
        {
            var items = NodeModel.GetCachedAstInput<object>(0, ModelEngineController);
            var serializedValues = NodeModel.SelectedValue.ToArray();
            if (NodeModel.SetItems(items))
                TryRestoreSelection(serializedValues);
        }

        private void TryRestoreSelection(string[] serializedSelection)
        {
            var restored = GlobalArgumentService.FilterBySerializationValue(NodeModel.Items.ToArray(), serializedSelection, false).Cast<AstReference>();
            DispatchUI(() =>
            {
                if (0 == _control?.SelectionListBox.SelectedItems.Count)
                {
                    foreach (var astReference in restored)
                        _control.SelectionListBox.SelectedItems.Add(astReference);
                }
            });
        }

        private void SelectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (sender as ListBox).SelectedItems.Cast<AstReference>().ToArray();            
            AsyncSchedule(() => NodeModel.SetSelected(selected, true));
        }

        private void Model_PortConnected(Dynamo.Graph.Nodes.PortModel arg1, Dynamo.Graph.Connectors.ConnectorModel arg2)
        {
            AsyncSchedule(UpdateItems);
        }

        private void Model_PortDisconnected(Dynamo.Graph.Nodes.PortModel obj)
        {
            AsyncSchedule(UpdateItems);
        }

        public override void Dispose()
        {
            if (null == _control)
                throw new ObjectDisposedException(nameof(SelectableItemsNodeCustomization));

            _control.SelectionListBox.SelectionChanged -= SelectionListBox_SelectionChanged;

            NodeModel.PortDisconnected -= Model_PortDisconnected;
            NodeModel.PortConnected -= Model_PortConnected;

            _control = null;
        }
    }

#pragma warning restore CS1591

}
