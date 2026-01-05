using Dynamo.Controls;

using TRex.Task;

namespace TRex.UI.Customization;


public class CancelableOptionCommandCustomization<TModel, TOption>
    : CancelableProgressingNodeCustomization<TModel, CancelableOptionCommandControl> where TModel : CancelableProgressingOptionNodeModel<TOption>
{
    protected CancelableOptionCommandCustomization(ProgressOnPortType progressOnPort)
        : base(progressOnPort)
    {
    }

    protected override CancelableOptionCommandControl CreateControl(TModel model, NodeView nodeView)
    {
        var cancelableControl = new CancelableOptionCommandControl();
        nodeView.inputGrid.Children.Add(cancelableControl);
        cancelableControl.DataContext = model;
        return cancelableControl;
    }

    protected override CancelableCommandControl? ProgressControl => Control?.ProgressCommandControl;
}