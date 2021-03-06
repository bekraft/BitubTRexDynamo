﻿using Dynamo.Controls;

using System.Linq;

using Validation;

namespace UI.Customization
{
#pragma warning disable CS1591

    public class IfcReportDomainComposingNodeCustomization : BaseNodeViewCustomization<IfcReportDomainComposingNodeModel>
    {
        private SelectableListControl _control;

        public override void CustomizeView(IfcReportDomainComposingNodeModel model, NodeView nodeView)
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
            NodeModel.Selected = _control.SelectionListBox.SelectedItems.Cast<IfcReportDomain>().ToArray();
        }

        public override void Dispose()
        {
            _control.SelectionListBox.SelectionChanged -= SelectionListBox_SelectionChanged;
            _control = null;
        }
    }

#pragma warning restore CS1591
}
