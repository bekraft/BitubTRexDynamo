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
            _dockCallbackQualifier = GlobalDelegationService.Put<NodeProgressing>(
                new[] { GetType().FullName, nameof(DockOnProgressing) }.ToQualifier(), DockOnProgressing);
        }

        protected CancelableProgressingNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            ResetState();
            _dockCallbackQualifier = GlobalDelegationService.Put<object>(
                new[] { GetType().FullName, nameof(DockOnProgressing) }.ToQualifier(), DockOnProgressing);
        }

        private void OnNodeProgessEnded(object sender, NodeProgressEndEventArgs args)
        {
            if (sender is NodeProgressing np)
            {
                np.OnProgressChange -= OnNodeProgressChanged;
                np.OnProgressEnd -= OnNodeProgessEnded;
            }
        }

        private void OnNodeProgressChanged(object sender, NodeProgressEventArgs args)
        {
            lock (_mutex)
            {
                ProgressPercentage = args.Percentage;
                ProgressState = args.State?.ToString() ?? args.TaskName;
                TaskName = args.TaskName;                
            }
        }

        internal object DockOnProgressing(object obj)
        {
            if (obj is NodeProgressing nodeProgressing)
            {
                nodeProgressing.OnProgressChange += OnNodeProgressChanged;
                nodeProgressing.OnProgressEnd += OnNodeProgessEnded;
            } 
            else
            {
                if (null != obj)
                    Warning($"Expected a progress event source but got '{obj.GetType()}'");
            }
            return obj;
        }

        protected AssociativeNode DockOnProgressing(AssociativeNode progressProducer)
        {
            return AstFactory.BuildFunctionCall(
                new Func<string, object, object>(GlobalDelegationService.Call),
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
                ProgressState = "ready";
                TaskName = "(not active)";
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
