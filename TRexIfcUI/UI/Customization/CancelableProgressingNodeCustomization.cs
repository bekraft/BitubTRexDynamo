using Dynamo.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

using Autodesk.DesignScript.Runtime;

using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using TRex.Task;
using TRex.Internal;

using Bitub.Dto;

using ProgressingPort = System.Tuple<Dynamo.Graph.Nodes.PortType, int, TRex.Internal.ProgressingTask[]>;

namespace TRex.UI.Customization;

[Flags]
[IsVisibleInDynamoLibrary(false)]
public enum ProgressOnPortType : int
{
    NoProgress = 0,
    InPorts = 1, 
    OutPorts = 2,
    AllPorts = 3
}

public abstract class CancelableProgressingNodeCustomization<T, V> 
    : BaseNodeViewCustomization<T> where T : CancelableProgressingNodeModel where V : UserControl
{
    #region Internals

    private readonly List<ProgressingPort> _taskProgressingOnPort = new List<ProgressingPort>();
    private V? _control;
    
    protected CancelableProgressingNodeCustomization(ProgressOnPortType progressOnPort)
    {
        ProgressOnPort = progressOnPort;
    }

    protected abstract V CreateControl(T model, NodeView nodeView);

    protected virtual V? Control => _control;

    protected virtual CancelableCommandControl? ProgressControl => _control as CancelableCommandControl;
    
    public ProgressOnPortType ProgressOnPort { get; private set; }

    public override void CustomizeView(T model, NodeView nodeView)
    {
        base.CustomizeView(model, nodeView);
        _control = CreateControl(model, nodeView);

        var pc = ProgressControl;
        if (null != pc)
        {
            pc.Cancel.IsEnabled = false;
            pc.Cancel.Click += (s, e) =>
            {
                pc.Cancel.IsEnabled = false;
                model.IsCanceled = true;
            };
        }
        
        model.ResetState();

        ModelEngineController!.LiveRunnerRuntimeCore.ExecutionEvent += LiveRunnerRuntimeCore_ExecutionEvent;

        model.PortConnected += NodeModel_PortConnected;
        model.PortDisconnected += NodeModel_PortDisconnected;
        model.Modified += NodeModel_Modified;
    }

    private void LiveRunnerRuntimeCore_ExecutionEvent(object? sender, ProtoCore.ExecutionStateEventArgs e)
    {
        switch(e.ExecutionState)
        {
            case ProtoCore.ExecutionStateEventArgs.State.ExecutionBegin:
                DispatchUI(() =>
                {
                    if (null != ProgressControl) ProgressControl.Cancel.IsEnabled = true;
                });
                
                if (NodeModel != null)
                {
                    NodeModel.ClearErrorsAndWarnings();
                    NodeModel.CancellationVisibility = System.Windows.Visibility.Visible;
                }

                lock (GlobalLogging.DiagnosticStopWatch)
                {
                    if (!GlobalLogging.DiagnosticStopWatch.IsRunning)
                        GlobalLogging.DiagnosticStopWatch.Restart();
                }
                break;

            case ProtoCore.ExecutionStateEventArgs.State.ExecutionEnd:
                if (NodeModel != null)
                {
                    NodeModel.CancellationVisibility = System.Windows.Visibility.Collapsed;
                    NodeModel.ResetState();
                }

                lock (GlobalLogging.DiagnosticStopWatch)
                    GlobalLogging.DiagnosticStopWatch.Stop();


                Store.ModelCache.Instance.ClearCompleteCache();

                break;
        }            
    }

    private void NodeModel_Modified(NodeModel obj)
    {
        NodeModel?.ResetState();
    }

    private void NodeModel_PortDisconnected(PortModel pm)
    {
        switch (pm.PortType)
        {
            case PortType.Input:
                if (ProgressOnPort.HasFlag(ProgressOnPortType.InPorts))
                    RemoveProgressTasks(pm.PortType, pm.Index).ForEach(RemoveEventHandlerFrom);
                break;
            case PortType.Output:
                if (ProgressOnPort.HasFlag(ProgressOnPortType.OutPorts))
                    RemoveProgressTasks(pm.PortType, pm.Index).ForEach(RemoveEventHandlerFrom);
                break;
        }

        NodeModel?.ResetState();
    }

    private void NodeModel_PortConnected(PortModel pm, ConnectorModel cm)
    {
        NodeModel?.ResetState();

        switch (pm.PortType)
        {
            case PortType.Input:
                if (ProgressOnPort.HasFlag(ProgressOnPortType.InPorts))
                    GetProgressingTasks(pm.PortType, pm.Index).ForEach(AddEventHandlerTo);
                break;
            case PortType.Output:
                if (ProgressOnPort.HasFlag(ProgressOnPortType.OutPorts))
                    GetProgressingTasks(pm.PortType, pm.Index).ForEach(AddEventHandlerTo);
                break;
        }
    }

    private IEnumerable<ProgressingPort> GetProgressingTasksOnPort(ProgressOnPortType progressOnPort, PortType? portType, int? portIndex)
    {
        IEnumerable<ProgressingPort> eventSources = Enumerable.Empty<ProgressingPort>();
        if (progressOnPort.HasFlag(ProgressOnPortType.InPorts))
            eventSources = eventSources.Concat(NodeModel?.InPorts
                .Where(p => (!portType.HasValue || portType == p.PortType) && (!portIndex.HasValue || portIndex == p.Index))
                .Select(p => new ProgressingPort(
                    p.PortType, 
                    p.Index, 
                    NodeModel.GetCachedInput<ProgressingTask>(p.Index, ModelEngineController).ToArray())) ?? Enumerable.Empty<ProgressingPort>());
        if (progressOnPort.HasFlag(ProgressOnPortType.OutPorts))
            eventSources = eventSources.Concat(NodeModel?.OutPorts
                .Where(p => (!portType.HasValue || portType == p.PortType) && (!portIndex.HasValue || portIndex == p.Index))
                .Select(p => new ProgressingPort(
                    p.PortType, 
                    p.Index, 
                    NodeModel.GetCachedOutput<ProgressingTask>(p.Index, ModelEngineController).ToArray())) ?? Enumerable.Empty<ProgressingPort>());

        return eventSources.Where(e => e.Item3.Length > 0).ToArray();
    }

    private void AddEventHandlerTo(ProgressingTask task)
    {
        if (null != NodeModel)
        {
            task.OnProgressChange += NodeModel.OnTaskProgressChanged;
            task.OnProgressEnd += NodeModel.OnTaskProgessEnded;

            if (task.LatestProgressEventArgs is NodeProgressEndEventArgs endEventArgs)
                NodeModel.OnTaskProgessEnded(task, endEventArgs);
        }
    }

    private void RemoveEventHandlerFrom(ProgressingTask task)
    {
        if (null != NodeModel)
        {
            task.OnProgressChange -= NodeModel.OnTaskProgressChanged;
            task.OnProgressEnd -= NodeModel.OnTaskProgessEnded;
        }
    }

    protected ProgressingTask[] GetProgressingTasks(PortType? portType = null, int? portIndex = null)
    {
        List<ProgressingTask> tasks = new List<ProgressingTask>();
        foreach (var np in GetProgressingTasksOnPort(ProgressOnPort, portType, portIndex))
        {
            tasks.AddRange(np.Item3);
            _taskProgressingOnPort.Add(np);                    
        }            
        return tasks.ToArray();
    }

    protected ProgressingTask[] RemoveProgressTasks(PortType? portType = null, int? portIndex = null)
    {
        List<ProgressingTask> tasks = new List<ProgressingTask>();
        var tasksOnPort = _taskProgressingOnPort.ToArray();

        for (int i=tasksOnPort.Length-1; i>=0; i--)
        {
            ProgressingPort pp = tasksOnPort[i];
            if ((!portType.HasValue || portType == pp.Item1) && (!portIndex.HasValue || portIndex == pp.Item2))
            {
                tasks.AddRange(pp.Item3);
                _taskProgressingOnPort.RemoveAt(i);
            }
        }
        return tasks.ToArray();
    }

    protected override void OnCachedValueChange(object? sender)
    {
        RemoveProgressTasks().ForEach(RemoveEventHandlerFrom);
        GetProgressingTasks().ForEach(AddEventHandlerTo);
    }

    #endregion

    public override void Dispose()
    {
        RemoveProgressTasks().ForEach(RemoveEventHandlerFrom);
        if (null != ModelEngineController)
        {
            ModelEngineController.LiveRunnerRuntimeCore.ExecutionEvent -= LiveRunnerRuntimeCore_ExecutionEvent;
        }
    }
}