using Dynamo.Controls;
using Dynamo.Wpf;

using Log;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class FileLogOptionsControlCustomization : INodeViewCustomization<FileLog>
    {
        public void CustomizeView(FileLog model, NodeView nodeView)
        {
            var logOptionsControl = new LogOptionsControl();
            nodeView.inputGrid.Children.Add(logOptionsControl);
            logOptionsControl.DataContext = model;
        }

        public void Dispose()
        {            
        }
    }

#pragma warning restore CS1591
}
