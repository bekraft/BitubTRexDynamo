using Dynamo.Wpf;
using Dynamo.Controls;

using Task;

// Disable comment warning
#pragma warning disable CS1591

namespace UI
{
    public class CancelableCommandNodeCustomization : INodeViewCustomization<CancelableCommandNodeModel>
    {
        public void CustomizeView(CancelableCommandNodeModel model, NodeView nodeView)
        {
            var cancelableControl = new CancelableCommandControl();
            nodeView.inputGrid.Children.Add(cancelableControl);
            cancelableControl.DataContext = model;            
            cancelableControl.Cancel.Click += (s, e) => model.IsCanceled = true;
        }

        public void Dispose()
        {            
        }
    }
}

#pragma warning restore CS1591
