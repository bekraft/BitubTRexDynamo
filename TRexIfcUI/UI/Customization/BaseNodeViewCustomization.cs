using System;
using Dynamo.ViewModels;
using Dynamo.Graph.Nodes;
using System.Windows.Threading;

using Dynamo.Controls;
using Dynamo.Engine;
using Dynamo.Wpf;
using Dynamo.Scheduler;

namespace TRex.UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public abstract class BaseNodeViewCustomization<T> : INodeViewCustomization<T> where T : NodeModel
    {
        #region Internals
        private DynamoViewModel viewModel;
        private DispatcherSynchronizationContext syncContext;
        private NodeView nodeView;

        protected T NodeModel { get; set; }
        #endregion

        public virtual void CustomizeView(T model, NodeView nodeView)
        {
            this.viewModel = nodeView.ViewModel.DynamoViewModel;
            this.nodeView = nodeView;
            this.syncContext = new DispatcherSynchronizationContext(nodeView.Dispatcher);
            
            NodeModel = model;

            NodeModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "CachedValue")
                    OnCachedValueChange(s);
            };
        }

        public abstract void Dispose();

        protected virtual void OnCachedValueChange(object sender)
        {
        }

        protected DelegateBasedAsyncTask AsyncThenUI(DelegateBasedAsyncTask task, Action action)
        {
            task.ThenSend((_) => action?.BeginInvoke(action.EndInvoke, null), syncContext);
            return task;
        }

        protected DelegateBasedAsyncTask AsyncTask(Action action)
        {
            return new DelegateBasedAsyncTask(viewModel.Model.Scheduler, action);
        }

        protected void AsyncSchedule(DelegateBasedAsyncTask task)
        {
            viewModel.Model.Scheduler.ScheduleForExecution(task);
        }

        protected void AsyncSchedule(Action action, Action thenUI = null)
        {
            AsyncSchedule(AsyncThenUI(AsyncTask(action), thenUI));
        }

        protected void DispatchUI(Action uiAction)
        {
            nodeView?.Dispatcher.BeginInvoke(uiAction, DispatcherPriority.Background);
        }

        protected EngineController ModelEngineController { get => viewModel.Model.EngineController; }

        protected DynamoScheduler ModelScheduler { get => viewModel.Model.Scheduler; }
    }

#pragma warning restore CS1591
}
