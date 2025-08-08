using Dynamo.Controls;
using Dynamo.Wpf;
using TRex.Map;

namespace TRex.UI.Customization;

public class MapConversionCustomization : INodeViewCustomization<MapConversionModel>
{
    public void CustomizeView(MapConversionModel model, NodeView nodeView)
    {
        var control = new MapConversionControl();
        nodeView.inputGrid.Children.Add(control);
        control.DataContext = model;
    }

    public void Dispose()
    {
    }
}