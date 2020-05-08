using Dynamo.Wpf;
using Dynamo.Controls;

using Task;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public abstract class CancelableProgressingOptionNodeCustomization<T>
        : CancelableProgressingNodeCustomization<T> where T : CancelableProgressingOptionNodeModel
    {
        protected CancelableProgressingOptionNodeCustomization(ProgressOnPortType progressOnPort)
            : base(progressOnPort)
        {
        }

        protected override void CreateView(T model, NodeView nodeView)
        {
            var cancelableControl = new CancelableOptionCommandControl();
            nodeView.inputGrid.Children.Add(cancelableControl);
            cancelableControl.DataContext = model;
            cancelableControl.ProgressCommandControl.Cancel.Click += (s, e) => model.IsCanceled = true;
        }
    }

#pragma warning restore CS1591
}