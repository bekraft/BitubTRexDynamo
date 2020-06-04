using Dynamo.Wpf;
using Dynamo.Controls;

using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using Task;
using Internal;

using Log;

using ProgressingPort = System.Tuple<Dynamo.Graph.Nodes.PortType, int, Internal.NodeProgressing[]>;

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
        public ProgressOnPortType ProgressOnPort { get; private set; }
        public LogReason ActionOnPort { get; private set; }

        private List<ProgressingPort> _nodeProgressingPort = new List<ProgressingPort>();
        private readonly object _monitor = new object();

        protected CancelableProgressingNodeCustomization(ProgressOnPortType progressOnPort, LogReason actionOnPort)
        {
            ProgressOnPort = progressOnPort;
            ActionOnPort = actionOnPort;
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
            ResetProgress();

            NodeModel.PortConnected += NodeModel_PortConnected;
            NodeModel.PortDisconnected += NodeModel_PortDisconnected;
        }

        private void NodeModel_PortDisconnected(PortModel pm)
        {
            int count = 0;
            switch (pm.PortType)
            {
                case PortType.Input:
                    if (ProgressOnPort.HasFlag(ProgressOnPortType.InPorts))
                        count += RemoveOnProgressChanging(pm.PortType, pm.Index).Length;
                    break;
                case PortType.Output:
                    if (ProgressOnPort.HasFlag(ProgressOnPortType.OutPorts))
                        count += RemoveOnProgressChanging(pm.PortType, pm.Index).Length;
                    break;
            }

            if (count > 0)
                ResetProgress();
        }

        protected virtual void ResetProgress()
        {
            NodeModel.ProgressPercentage = 0;
            NodeModel.ProgressState = "";
            NodeModel.TaskName = "(no active task)";
        }

        private void NodeModel_PortConnected(PortModel pm, ConnectorModel cm)
        {
            switch(pm.PortType)
            {
                case PortType.Input:
                    if (ProgressOnPort.HasFlag(ProgressOnPortType.InPorts))
                        AddOnProgressChanging(pm.PortType, pm.Index);
                    break;
                case PortType.Output:
                    if (ProgressOnPort.HasFlag(ProgressOnPortType.OutPorts))
                        AddOnProgressChanging(pm.PortType, pm.Index);
                    break;
            }
        }

        private IEnumerable<ProgressingPort> GetNodeProgressing(ProgressOnPortType progressOnPort, PortType? portType, int? portIndex)
        {
            IEnumerable<ProgressingPort> eventSources = Enumerable.Empty<ProgressingPort>();
            if (progressOnPort.HasFlag(ProgressOnPortType.InPorts))
                eventSources = eventSources.Concat(NodeModel.InPorts
                    .Where(p => (!portType.HasValue || portType == p.PortType) && (!portIndex.HasValue || portIndex == p.Index))
                    .Select(p => new ProgressingPort(
                        p.PortType, 
                        p.Index, 
                        NodeModel.GetCachedInput<NodeProgressing>(p.Index, ModelEngineController).Where(n => n != null).ToArray())));
            if (progressOnPort.HasFlag(ProgressOnPortType.OutPorts))
                eventSources = eventSources.Concat(NodeModel.OutPorts
                    .Where(p => (!portType.HasValue || portType == p.PortType) && (!portIndex.HasValue || portIndex == p.Index))
                    .Select(p => new ProgressingPort(
                        p.PortType, 
                        p.Index, 
                        NodeModel.GetCachedOutput<NodeProgressing>(p.Index, ModelEngineController).Where(n => n != null).ToArray())));

            return eventSources.Where(e => e.Item3.Length > 0).ToArray();
        }

        protected NodeProgressing[] AddOnProgressChanging(PortType? portType = null, int? portIndex = null)
        {
            List<NodeProgressing> eventSources = new List<NodeProgressing>();
            lock (_monitor)
            {
                foreach (var np in GetNodeProgressing(ProgressOnPort, portType, portIndex))
                {
                    foreach (var eventSource in np.Item3)
                    {
                        eventSource.OnProgressChange += OnProgressChanged;
                        eventSources.Add(eventSource);
                    }
                    
                    _nodeProgressingPort.Add(np);                    
                }
            }
            return eventSources.ToArray();
        }

        protected NodeProgressing[] RemoveOnProgressChanging(PortType? portType = null, int? portIndex = null)
        {
            List<NodeProgressing> eventSources = new List<NodeProgressing>();
            lock (_monitor)
            {                
                foreach (var np in _nodeProgressingPort
                    .Where(n => (!portType.HasValue || portType == n.Item1) && (!portIndex.HasValue || portIndex == n.Item2)))
                {
                    foreach (var eventSource in np.Item3)
                    {
                        eventSource.OnProgressChange -= OnProgressChanged;
                        eventSources.Add(eventSource);
                    }
                }

                // Filtering for remaining port connections
                _nodeProgressingPort = _nodeProgressingPort
                    .Where(n => (portType.HasValue && portType != n.Item1) || (portIndex.HasValue && portIndex != n.Item2))
                    .ToList();
            }
            return eventSources.ToArray();
        }

        protected override void OnCachedValueChange(object sender)
        {
            // Remove first (since the internal event sources might have changed)
            foreach (var eventSource in RemoveOnProgressChanging())
                eventSource.ClearState();

            // Add any newly attached event source is unknown => clear state before (since it might be changed)
            AddOnProgressChanging();
        }

        private void OnProgressChanged(object sender, NodeProgressingEventArgs e)
        {
            if (ActionOnPort.HasFlag(e.Action))
            {
                NodeModel.ProgressPercentage = e.Percentage;
                NodeModel.ProgressState = e.State?.ToString() ?? e.TaskName;
                NodeModel.TaskName = e.TaskName ?? e.State?.ToString();
            }
        }

        public override void Dispose()
        {
            RemoveOnProgressChanging();            
        }
    }

    // Disable comment warning
#pragma warning restore CS1591

}