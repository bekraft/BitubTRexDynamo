using System;
using Dynamo.ViewModels;
using Dynamo.Graph.Nodes;
using System.Windows.Threading;

using Dynamo.Controls;
using Dynamo.Engine;
using Dynamo.Wpf;
using Dynamo.Scheduler;

namespace TRex.UI.Customization;

public abstract class BaseNodeViewCustomization<T> : INodeViewCustomization<T> where T : NodeModel
{
    #region Internals
    private DynamoViewModel? _viewModel;
    private DispatcherSynchronizationContext? _syncContext;
    private NodeView? _nodeView;

    #endregion
    
    public virtual void CustomizeView(T model, NodeView nodeView)
    {
        _viewModel = nodeView.ViewModel.DynamoViewModel;
        _nodeView = nodeView;
        _syncContext = new DispatcherSynchronizationContext(nodeView.Dispatcher);
        
        NodeModel = model;

        NodeModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "CachedValue")
            {
                OnCachedValueChange(s);                    
            };
        };
    }

    public abstract void Dispose();
    
    protected T? NodeModel { get; private set; }

    protected virtual void OnCachedValueChange(object? sender)
    {
    }

    protected DelegateBasedAsyncTask AsyncThenUI(DelegateBasedAsyncTask task, Action? action)
    {
        task.ThenSend((_) => action?.Invoke(), _syncContext);
        return task;
    }

    protected DelegateBasedAsyncTask AsyncTask(Action action)
    {
        return new DelegateBasedAsyncTask(_viewModel?.Model.Scheduler, action);
    }

    protected void AsyncSchedule(DelegateBasedAsyncTask task)
    {
        _viewModel?.Model.Scheduler.ScheduleForExecution(task);
    }

    protected void AsyncSchedule(Action action, Action? thenUI = null)
    {
        AsyncSchedule(AsyncThenUI(AsyncTask(action), thenUI));
    }

    protected void DispatchUI(Action uiAction)
    {
        _nodeView?.Dispatcher.InvokeAsync(uiAction, DispatcherPriority.Background);
    }

    protected EngineController? ModelEngineController => _viewModel?.Model.EngineController;

    protected DynamoScheduler? ModelScheduler => _viewModel?.Model.Scheduler;
}
