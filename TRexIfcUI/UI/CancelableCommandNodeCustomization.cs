using Dynamo.Wpf;
using Dynamo.Controls;

// Disable comment warning
#pragma warning disable CS1591

namespace TRexIfc.UI
{
    public class CancelableCommandNodeCustomization : INodeViewCustomization<CancelableCommandNode>
    {
        public void CustomizeView(CancelableCommandNode model, NodeView nodeView)
        {
            var cancelableControl = new CancelableCommandControl();
            nodeView.inputGrid.Children.Add(cancelableControl);
            cancelableControl.DataContext = model;
            model.TaskName = "(Initiated)";
            cancelableControl.Cancel.Click += (s, e) => model.IsCanceled = true;
        }

        public void Dispose()
        {            
        }
    }
}

#pragma warning restore CS1591
