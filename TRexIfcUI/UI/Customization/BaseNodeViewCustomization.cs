using Dynamo.Graph.Connectors;

using Dynamo.Controls;
using Dynamo.Engine;
using Dynamo.Wpf;
using Dynamo.Scheduler;

using Export;
using Store;

using System;
using System.Windows.Controls;
using Dynamo.ViewModels;
using Dynamo.Graph.Nodes;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public abstract class BaseNodeViewCustomization<T> : INodeViewCustomization<T> where T : NodeModel
    {
        private DynamoViewModel _viewModel;
        private DispatcherSynchronizationContext _syncContext;
        private NodeView _nodeView;
        protected T NodeModel { get; set; }

        public virtual void CustomizeView(T model, NodeView nodeView)
        {
            _viewModel = nodeView.ViewModel.DynamoViewModel;
            _nodeView = nodeView;
            _syncContext = new DispatcherSynchronizationContext(nodeView.Dispatcher);
            
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

        protected void ScheduleAsync(Action modelAction, Action uiAction = null)
        {
            var chain = new DelegateBasedAsyncTask(_viewModel.Model.Scheduler, modelAction);
            chain.ThenSend((_) => uiAction?.Invoke(), _syncContext);
            _viewModel.Model.Scheduler.ScheduleForExecution(chain);
        }

        protected void DispatchUI(Action uiAction)
        {
            _nodeView?.Dispatcher.Invoke(uiAction);
        }

        protected EngineController ModelEngineController { get => _viewModel.Model.EngineController; }

        protected DynamoScheduler ModelScheduler { get => _viewModel.Model.Scheduler; }
    }

#pragma warning restore CS1591
}
