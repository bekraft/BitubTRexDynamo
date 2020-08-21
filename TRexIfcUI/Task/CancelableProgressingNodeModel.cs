using System;
using System.Collections.Generic;
using System.Windows;
using Dynamo.Graph.Nodes;

using Newtonsoft.Json;

using Bitub.Transfer;

using Internal;
using ProtoCore.AST.AssociativeAST;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.ObjectModel;
using UI;

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
        private bool __isCanceled;
        private int _progressPercentage;
        private string _progressState;
        private string _taskName;
        private Visibility _visibility;

        private object _mutex = new object();
        #endregion

        protected CancelableProgressingNodeModel() : base()
        {
            ResetState();
            DynamicDelegation.Put<ProgressingTask, ProgressingTask>(ProgressingTaskMethodName, ConsumeAstProgressingTask);
        }

        protected CancelableProgressingNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            ResetState();
            DynamicDelegation.Put<ProgressingTask, ProgressingTask>(ProgressingTaskMethodName, ConsumeAstProgressingTask);
        }

        internal protected string[] ProgressingTaskMethodName
        {
            get => GetType().ToQualifiedMethodName(nameof(ConsumeAstProgressingTask));
        }

        internal void OnTaskProgessEnded(object sender, NodeProgressEndEventArgs args = null)
        {
            if (sender is ProgressingTask task)
            {
                task.OnProgressChange -= OnTaskProgressChanged;
                task.OnProgressEnd -= OnTaskProgessEnded;

                DispatchOnUIThread(() => ActiveTasks.Add(new ProgressingTaskInfo(task)));
            }
        }

        internal void OnTaskProgressChanged(object sender, NodeProgressEventArgs args)
        {
            lock (_mutex)
            {
                ProgressPercentage = args.Percentage;
                ProgressState = args.State?.ToString() ?? args.TaskName;
                TaskName = args.TaskName;

                if (null != args.InternalState)
                {
                    if (__isCanceled && !args.InternalState.IsAboutCancelling)
                        args.InternalState.MarkCancelling();

                    if (args.InternalState.IsCanceled)
                        IsCanceled = true;
                }
            }
        }

        [JsonIgnore]
        public ObservableCollection<ProgressingTaskInfo> ActiveTasks { get; } = new ObservableCollection<ProgressingTaskInfo>();

        public virtual ProgressingTask ConsumeAstProgressingTask(ProgressingTask obj)
        {
            if (null != obj)
            {
                obj.OnProgressChange += OnTaskProgressChanged;
                obj.OnProgressEnd += OnTaskProgessEnded;
            }
            else
            {
                throw new ArgumentNullException();
            }

            return obj;
        }

        protected virtual void BeforeBuildOutputAst()
        {
            ClearErrorsAndWarnings();
            ActiveTasks.Clear();
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
                lock (_mutex)
                    return __isCanceled;
            }
            set {
                lock (_mutex)
                    __isCanceled = value;

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
