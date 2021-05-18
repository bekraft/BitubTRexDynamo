using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows;
using System.Runtime.CompilerServices;

using Dynamo.Graph.Nodes;

using Newtonsoft.Json;

using Bitub.Dto;

using TRex.Internal;
using TRex.UI;
using TRex.UI.Model;
using TRex.Log;

// Disable comment warning
#pragma warning disable CS1591

namespace TRex.Task
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
        private Visibility _visibility = Visibility.Collapsed;

        private readonly object _mutex = new object();

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

        [JsonIgnore]
        internal LogReason LogReasonMask { get; set; } = LogReason.Any;

        internal void OnTaskProgessEnded(object sender, NodeProgressEndEventArgs args = null)
        {
            if (LogReason.None != (LogReasonMask & args.Reason))
            {
                if (sender is ProgressingTask task)
                {
                    DispatchCreateOrUpdate(task);
                }
            }
        }

        internal void OnTaskProgressChanged(object sender, NodeProgressEventArgs args)
        {
            if (LogReason.None != (LogReasonMask & args.Reason))
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
                    }

                    if (sender is ProgressingTask task)
                    {
                        if (__isCanceled)
                            task.CancelAll();
                    }
                }
            }
        }

        [JsonIgnore]
        public ObservableCollection<ProgressingTaskInfo> ActiveTasks 
        { 
            get; 
        } = new ObservableCollection<ProgressingTaskInfo>();

        public ProgressingTaskInfo[] ActiveTasksSafeCopy()
        {
            lock (_mutex)
                return ActiveTasks.ToArray();
        }

        public void ClearActiveTaskList()
        {
            lock (_mutex)
                ActiveTasks.Clear();
        }

        public ProgressingTaskInfo FindActiveTaskInfo(ProgressingTask task)
        {
            lock (_mutex)
                return ActiveTasks.FirstOrDefault(taskInfo => ReferenceEquals(taskInfo.Task, task));
        }

        public void DispatchCreateOrUpdate(ProgressingTask task)
        {
            DispatchOnUIThread(() =>
            {
                ProgressingTaskInfo taskInfo;
                if (!TryCreateActiveTaskInfo(task, out taskInfo))
                    taskInfo.Update();
            });
        }

        public bool TryCreateActiveTaskInfo(ProgressingTask task, out ProgressingTaskInfo taskInfo)
        {
            lock (_mutex)
            {
                taskInfo = ActiveTasks.FirstOrDefault(taskInfo => ReferenceEquals(taskInfo.Task, task));
                if (null == taskInfo)
                {
                    var newTaskInfo = new ProgressingTaskInfo(task);
                    taskInfo = newTaskInfo;
                    ActiveTasks.Add(newTaskInfo);
                    return true;
                }
                return false;
            }            
        }

        public virtual ProgressingTask ConsumeAstProgressingTask(ProgressingTask task)
        {
            if (null != task)
            {
                task.OnProgressChange += OnTaskProgressChanged;
                task.OnProgressEnd += OnTaskProgessEnded;
                DispatchCreateOrUpdate(task);               
            }
            return task;
        }

        protected virtual void BeforeBuildOutputAst()
        {
            ClearErrorsAndWarnings();
            ClearActiveTaskList();
            ResetState();
        }

        [JsonIgnore]
        public Visibility CancellationVisibility
        {
            get {
                return _visibility;
            }
            set {
                if (value != Visibility.Hidden && !IsCancelable)
                {
                    Log($"{Name} is not cancelable. No cancel button available.");
                }
                else
                {
                    _visibility = value;
                    RaisePropertyChanged(nameof(CancellationVisibility));
                }
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
                CancellationVisibility = value ? Visibility.Visible : Visibility.Collapsed;
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

                ActiveTasksSafeCopy().ForEach(t => t.Task.CancelAll());                
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
            lock (_mutex)
            {
                ProgressPercentage = 0;
                ProgressState = DEFAULT_PROGRESS_STATE;
                TaskName = DEFAULT_TASK_NAME;
            }

            CancellationVisibility = Visibility.Collapsed;
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
