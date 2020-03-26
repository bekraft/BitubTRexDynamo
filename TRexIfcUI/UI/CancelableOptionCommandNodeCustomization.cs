using Dynamo.Wpf;
using Dynamo.Controls;

using Task;

// Disable comment warning
#pragma warning disable CS1591

namespace UI
{
    public class CancelableOptionCommandNodeCustomization : INodeViewCustomization<CancelableOptionCommandNode>
    {
        public void CustomizeView(CancelableOptionCommandNode model, NodeView nodeView)
        {
            var cancelableControl = new CancelableOptionCommandControl();
            nodeView.inputGrid.Children.Add(cancelableControl);
            cancelableControl.DataContext = model;            
            cancelableControl.ProgressCommandControl.Cancel.Click += (s, e) => model.IsCanceled = true;
        }

        public void Dispose()
        {            
        }
    }
}

#pragma warning restore CS1591
