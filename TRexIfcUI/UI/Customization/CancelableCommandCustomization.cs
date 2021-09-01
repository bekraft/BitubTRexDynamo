﻿using Dynamo.Controls;

using TRex.Task;

using TRex.Log;

namespace TRex.UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class CancelableCommandCustomization<T> : CancelableProgressingNodeCustomization<T, CancelableCommandControl> where T : CancelableProgressingNodeModel
    {
        protected CancelableCommandCustomization(ProgressOnPortType progressOnPort)
            : base(progressOnPort)
        {
        }

        protected override CancelableCommandControl CreateControl(T model, NodeView nodeView)
        {
            var cancelableControl = new CancelableCommandControl();
            nodeView.inputGrid.Children.Add(cancelableControl);

            cancelableControl.DataContext = model;

            return cancelableControl;
        }

        protected override CancelableCommandControl ProgressControl { get => Control; }
    }

#pragma warning restore CS1591
}