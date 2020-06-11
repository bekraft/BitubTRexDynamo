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

            var selectedItems = NodeModel.SelectByValues(NodeModel.SelectedValue.ToArray());
            DispatchUI(() => 
            {
                foreach (var s in selectedItems)
                    _control.SelectionListBox.SelectedItems.Add(s);
            });
            ScheduleAsync(() => NodeModel.SetSelected(selectedItems, false));

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
            NodeModel.SetItems(items);
        }

        private void SelectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (sender as ListBox).SelectedItems.Cast<AstReference>().ToArray();
            ScheduleAsync(() => NodeModel.SetSelected(selected, true));
        }

        private void Model_PortConnected(Dynamo.Graph.Nodes.PortModel arg1, Dynamo.Graph.Connectors.ConnectorModel arg2)
        {
            ScheduleAsync(UpdateItems);
        }

        private void Model_PortDisconnected(Dynamo.Graph.Nodes.PortModel obj)
        {
            ScheduleAsync(UpdateItems);
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
