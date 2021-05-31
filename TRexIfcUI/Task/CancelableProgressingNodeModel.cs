using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows;

using Dynamo.Graph.Nodes;

using Newtonsoft.Json;

using Bitub.Dto;

using TRex.Internal;
using TRex.Log;
using TRex.UI.Model;

// Disable comment warning
#pragma warning disable CS1591

namespace TRex.Task
{
    public abstract class CancelableProgressingNodeModel : BaseNodeModel, ICancelableTaskNode
    {
        const string DEFAULT_PROGRESS_STATE = "(inactive)";
        const string DEFAULT_TASK_NAME = "(no tasks progressing)";

        #region Internals
        private bool isCancelable;
        private bool mIsCanceled;
        private int progressPercentage;
        private string progressState;
        private string taskName;
        private Visibility visibility = Visibility.Collapsed;

        private readonly object mutex = new object();

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
                lock (mutex)
                {
                    ProgressPercentage = args.Percentage;
                    ProgressState = args.State?.ToString() ?? args.TaskName;
                    TaskName = args.TaskName;

                    if (null != args.InternalState)
                    {
                        if (mIsCanceled && !args.InternalState.IsAboutCancelling)
                            args.InternalState.MarkCancelling();
                    }

                    if (sender is ProgressingTask task)
                    {
                        if (mIsCanceled)
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
            lock (mutex)
                return ActiveTasks.ToArray();
        }

        public void ClearActiveTaskList()
        {
            lock (mutex)
                ActiveTasks.Clear();
        }

        public ProgressingTaskInfo FindActiveTaskInfo(ProgressingTask task)
        {
            lock (mutex)
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
            lock (mutex)
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
                return visibility;
            }
            set {
                if (value != Visibility.Hidden && !IsCancelable)
                {
                    Log($"{Name} is not cancelable. No cancel button available.");
                }
                else
                {
                    visibility = value;
                    RaisePropertyChanged(nameof(CancellationVisibility));
                }
            }
        }

        public bool IsCancelable
        {
            get {
                return isCancelable;
            }
            set {
                isCancelable = value;
                RaisePropertyChanged(nameof(IsCancelable));
                CancellationVisibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        [JsonIgnore]
        public bool IsCanceled
        {
            get {                
                lock (mutex)
                    return mIsCanceled;
            }
            set {
                lock (mutex)
                    mIsCanceled = value;

                RaisePropertyChanged(nameof(IsCanceled));

                ActiveTasksSafeCopy().ForEach(t => t.Task.CancelAll());                
            }
        }

        [JsonIgnore]
        public string ProgressState
        {
            get {
                return progressState;
            }
            set {
                progressState = value;
                RaisePropertyChanged(nameof(ProgressState));                
            }
        }

        [JsonIgnore]
        public string TaskName
        {
            get {
                return taskName;
            }
            set {
                taskName = value;
                RaisePropertyChanged(nameof(TaskName));                
            }
        }

        [JsonIgnore]
        public int ProgressPercentage
        {
            get {
                return progressPercentage;
            }
            set {
                progressPercentage = System.Math.Max(0, System.Math.Min(100, value));
                RaisePropertyChanged(nameof(ProgressPercentage));
            }
        }

        public void ResetState()
        {
            lock (mutex)
            {
                ProgressPercentage = 0;
                ProgressState = DEFAULT_PROGRESS_STATE;
                TaskName = DEFAULT_TASK_NAME;
            }

            CancellationVisibility = Visibility.Collapsed;
        }

        public void Report(int percentage, object userState)
        {
            lock(mutex)
            {
                ProgressPercentage = percentage;
                ProgressState = $"{userState?.ToString() ?? "Running"}";
            }
        }

        public void Report(ProgressStateToken value)
        {
            lock (mutex)
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
