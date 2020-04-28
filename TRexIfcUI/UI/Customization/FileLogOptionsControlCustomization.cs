using Dynamo.Controls;
using Dynamo.Wpf;

using Log;

// Disable comment warning
#pragma warning disable CS1591

namespace UI
{
    public class FileLogOptionsControlCustomization : INodeViewCustomization<FileLog>
    {
        public void CustomizeView(FileLog model, NodeView nodeView)
        {
            var logOptionsControl = new LogOptionsControl();
            /*
            nodeView.inputGrid.RowDefinitions.Add(
                new System.Windows.Controls.RowDefinition
                {
                    Height = new System.Windows.GridLength()
                }
            );
            */
            nodeView.inputGrid.Children.Add(logOptionsControl);
            logOptionsControl.DataContext = model;
        }

        public void Dispose()
        {            
        }
    }
}

#pragma warning restore CS1591
