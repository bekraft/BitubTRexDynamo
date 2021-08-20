using System;
using System.Collections.Generic;
using System.Linq;

using System.Windows.Controls;

using Dynamo.Controls;
using TRex.Internal;

using TRex.Task;

namespace TRex.UI.Customization
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

            // TODO Add manually withou UI dispatch?
            //UpdateItems(NodeModel.SelectedValue.Select(v => new AstValue<object>(v)));
            InitSelection(NodeModel.SelectedValue.Select(v => new AstValue<object>(v)));

            model.PortDisconnected += Model_PortDisconnected;
            model.PortConnected += Model_PortConnected;

            _control.SelectionListBox.SelectionChanged += SelectionListBox_SelectionChanged;
        }

        protected override void OnCachedValueChange(object sender)
        {
            UpdateItems(NodeModel.GetCachedAstInput<object>(0, ModelEngineController));
        }

        private void UpdateItems(IEnumerable<AstValue<object>> items)
        {
            var serializedValues = NodeModel.SelectedValue.ToArray();
            if (NodeModel.SetItems(items.ToArray()))
                TryRestoreSelection(serializedValues);
        }

        private void InitSelection(IEnumerable<AstValue<object>> values)
        {
            if (NodeModel.SetItems(values.ToArray()))
            {
                foreach (var s in values)
                    _control.SelectionListBox.SelectedItems.Add(s);
            }
        }

        private void TryRestoreSelection(string[] serializedSelection)
        {
            var restored = DynamicArgumentDelegation.FilterBySerializationValue(NodeModel.Items.ToArray(), serializedSelection, false).Cast<AstReference>();
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
            AsyncSchedule(() => UpdateItems(NodeModel.GetCachedAstInput<object>(0, ModelEngineController)));
        }

        private void Model_PortDisconnected(Dynamo.Graph.Nodes.PortModel obj)
        {
            AsyncSchedule(() => UpdateItems(NodeModel.GetCachedAstInput<object>(0, ModelEngineController)));
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
