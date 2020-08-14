using System;
using System.Collections.Generic;
using System.Windows;
using Dynamo.Graph.Nodes;

using Newtonsoft.Json;

using Bitub.Transfer;

using Internal;
using ProtoCore.AST.AssociativeAST;
using System.Runtime.CompilerServices;

// Disable comment warning
#pragma warning disable CS1591

[assembly: InternalsVisibleTo("TRexIfc")]

namespace Task
{
    public abstract class CancelableProgressingNodeModel : BaseNodeModel, ICancelableTaskNode
    {
        const string DEFAULT_PROGRESS_STATE = "(inactive)";
        const string DEFAULT_TASK_NAME = "(no tasks progressing)";

        #region Internals
        private bool _isCancelable;
        private bool _isCanceled;
        private int _progressPercentage;
        private string _progressState;
        private string _taskName;
        private Visibility _visibility;

        private object _mutex = new object();
        private readonly string _dockCallbackQualifier;
        #endregion

        protected CancelableProgressingNodeModel() : base()
        {
            ResetState();
            _dockCallbackQualifier = DynamicDelegation.Put<ProgressingTask>(
                new[] { GetType().FullName, nameof(BuildAstNodeProgressMonitor) }.ToQualifier(), DockProgressMonitor);
        }

        protected CancelableProgressingNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            ResetState();
            _dockCallbackQualifier = DynamicDelegation.Put<object>(
                new[] { GetType().FullName, nameof(BuildAstNodeProgressMonitor) }.ToQualifier(), DockProgressMonitor);
        }

        internal void OnNodeProgessEnded(object sender, NodeProgressEndEventArgs args = null)
        {
            if (sender is ProgressingTask np)
            {
                np.OnProgressChange -= OnNodeProgressChanged;                
            }
        }

        internal void OnNodeProgressChanged(object sender, NodeProgressEventArgs args)
        {
            lock (_mutex)
            {
                ProgressPercentage = args.Percentage;
                ProgressState = args.State?.ToString() ?? args.TaskName;
                TaskName = args.TaskName;                
            }
        }

        public object DockProgressMonitor(object obj)
        {
            if (obj is ProgressingTask nodeProgressing)
            {
                nodeProgressing.OnProgressChange += OnNodeProgressChanged;
            } 
            else
            {
                if (null != obj)
                    Warning($"Expected a progress event source but got '{obj.GetType()}'");
            }
            return obj;
        }

        protected AssociativeNode BuildAstNodeProgressMonitor(AssociativeNode progressProducer)
        {
            return AstFactory.BuildFunctionCall(
                new Func<string, object, object>(DynamicDelegation.Call),
                new List<AssociativeNode>()
                {
                    AstFactory.BuildStringNode(_dockCallbackQualifier),
                    progressProducer
                });
        }

        [JsonIgnore]
        public Visibility CancellationVisibility
        {
            get {
                return _visibility;
            }
            set {
                _visibility = value;
                RaisePropertyChanged(nameof(CancellationVisibility));
            }
        }

        public bool IsCancelable
        {
            get {
                return _isCancelable;
            }
            set {
                _isCancelable = value;
                RaisePropertyChanged(nameof(IsCancelable));
            }
        }

        [JsonIgnore]
        public bool IsCanceled
        {
            get {
                return _isCanceled;
            }
            set {
                _isCanceled = value;
                RaisePropertyChanged(nameof(IsCanceled));
            }
        }

        [JsonIgnore]
        public string ProgressState
        {
            get {
                return _progressState;
            }
            set {
                _progressState = value;
                RaisePropertyChanged(nameof(ProgressState));                
            }
        }

        [JsonIgnore]
        public string TaskName
        {
            get {
                return _taskName;
            }
            set {
                _taskName = value;
                RaisePropertyChanged(nameof(TaskName));                
            }
        }

        [JsonIgnore]
        public int ProgressPercentage
        {
            get {
                return _progressPercentage;
            }
            set {
                _progressPercentage = System.Math.Max(0, System.Math.Min(100, value));
                RaisePropertyChanged(nameof(ProgressPercentage));
            }
        }

        public void ResetState()
        {
            ClearErrorsAndWarnings();
            lock (_mutex)
            {
                ProgressPercentage = 0;
                ProgressState = DEFAULT_PROGRESS_STATE;
                TaskName = DEFAULT_TASK_NAME;
            }
        }

        public void Report(int percentage, object userState)
        {
            lock(_mutex)
            {
                ProgressPercentage = percentage;
                ProgressState = $"{userState?.ToString() ?? "Running"}";
            }
        }

        public void Report(ProgressStateToken value)
        {
            lock (_mutex)
            {
                var percentage = value.Percentage;
                ProgressPercentage = percentage;
                ProgressState = $"{percentage}%";

                if (IsCancelable && IsCanceled)
                    value.MarkCanceled();
            }
        }
    }
}

#pragma warning restore CS1591
