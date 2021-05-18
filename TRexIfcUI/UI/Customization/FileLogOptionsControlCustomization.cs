using Dynamo.Controls;
using Dynamo.Wpf;

using TRex.Log;

namespace TRex.UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class FileLogOptionsControlCustomization : BaseNodeViewCustomization<FileLog>
    {       
        public override void CustomizeView(FileLog model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);

            var logOptionsControl = new LogOptionsControl();
            nodeView.inputGrid.Children.Add(logOptionsControl);
            logOptionsControl.DataContext = model;
        }

        public override void Dispose()
        {            
        }
    }

#pragma warning restore CS1591
}
