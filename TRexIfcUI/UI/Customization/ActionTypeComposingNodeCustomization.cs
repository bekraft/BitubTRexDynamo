using Dynamo.Controls;
using Dynamo.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Log;

namespace UI.Customization
{
#pragma warning disable CS1591

    public class ActionTypeComposingNodeCustomization : BaseNodeViewCustomization<ActionTypeComposingNodeModel>
    {
        private SelectableListControl _control;

        public override void CustomizeView(ActionTypeComposingNodeModel model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);

            _control = new SelectableListControl();
            nodeView.inputGrid.Children.Add(_control);
            _control.DataContext = model;

            foreach (var flag in model.Selected)
                _control.SelectionListBox.SelectedItems.Add(flag);

            _control.SelectionListBox.SelectionChanged += SelectionListBox_SelectionChanged;
        }

        private void SelectionListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            NodeModel.Selected = _control.SelectionListBox.SelectedItems.Cast<ActionType>().ToArray();
        }

        public override void Dispose()
        {
            _control.SelectionListBox.SelectionChanged -= SelectionListBox_SelectionChanged;
            _control = null;
        }
    }

#pragma warning restore CS1591
}
