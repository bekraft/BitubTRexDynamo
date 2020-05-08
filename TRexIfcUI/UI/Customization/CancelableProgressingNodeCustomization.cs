using Dynamo.Wpf;
using Dynamo.Controls;

using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

using Task;
using Internal;
using Autodesk.DesignScript.Runtime;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    [Flags]
    [IsVisibleInDynamoLibrary(false)]
    public enum ProgressOnPortType : int
    {
        NoProgress = 0,
        InPorts = 1, 
        OutPorts = 2,
        AllPorts = 3
    }

    public abstract class CancelableProgressingNodeCustomization<T> 
        : BaseNodeViewCustomization<T> where T : CancelableProgressingNodeModel
    {
        // TODO  Port name vs. ActionType
        public ProgressOnPortType ProgressOnPort { get; private set; }

        protected CancelableProgressingNodeCustomization(ProgressOnPortType progressOnPort = ProgressOnPortType.AllPorts)
        {
            ProgressOnPort = progressOnPort;
        }

        protected virtual void CreateView(T model, NodeView nodeView)
        {
            var cancelableControl = new CancelableCommandControl();
            nodeView.inputGrid.Children.Add(cancelableControl);
            cancelableControl.DataContext = model;
            cancelableControl.Cancel.Click += (s, e) => model.IsCanceled = true;
        }

        public override void CustomizeView(T model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);
            CreateView(model, nodeView);
        }

        protected override void OnCachedValueChange(object sender)
        {
            IEnumerable<NodeProgressing> eventSources = Enumerable.Empty<NodeProgressing>();
            if (ProgressOnPort.HasFlag(ProgressOnPortType.InPorts))
                eventSources = eventSources.Concat(NodeModel.InPorts.SelectMany(p =>
                    NodeModel.GetCachedInput<NodeProgressing>(p.Index, ModelEngineController)));
            if (ProgressOnPort.HasFlag(ProgressOnPortType.OutPorts))
                eventSources = eventSources.Concat(NodeModel.OutPorts.SelectMany(p =>
                    NodeModel.GetCachedOutput<NodeProgressing>(p.Index, ModelEngineController)));

            foreach (var eventSource in eventSources.Where(e => null != e))
            {
                eventSource.OnProgressChange += OnProgressChanged;
            }
        }

        private void OnProgressChanged(object sender, NodeProgressingEventArgs e)
        {
            NodeModel.ProgressPercentage = e.Percentage;
            NodeModel.ProgressState = e.State?.ToString() ?? e.TaskName;
            NodeModel.TaskName = e.TaskName ?? e.State?.ToString();
        }
    }

    // Disable comment warning
#pragma warning restore CS1591

}