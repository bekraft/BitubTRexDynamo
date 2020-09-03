using Dynamo.Controls;

using Task;
using Log;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class CancelableOptionCommandCustomization<T>
        : CancelableProgressingNodeCustomization<T, CancelableOptionCommandControl> where T : CancelableProgressingOptionNodeModel
    {
        protected CancelableOptionCommandCustomization(ProgressOnPortType progressOnPort, LogReason actionOnPort)
            : base(progressOnPort, actionOnPort)
        {
        }

        protected override CancelableOptionCommandControl CreateControl(T model, NodeView nodeView)
        {
            var cancelableControl = new CancelableOptionCommandControl();
            nodeView.inputGrid.Children.Add(cancelableControl);
            cancelableControl.DataContext = model;
            return cancelableControl;
        }

        protected override CancelableCommandControl ProgressControl => Control.ProgressCommandControl;
    }

#pragma warning restore CS1591
}