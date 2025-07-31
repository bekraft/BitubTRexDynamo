using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading;
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
        const string DefaultProgressState = "(inactive)";
        const string DefaultTaskName = "(no tasks progressing)";
        const int TimeoutMilliseconds = 1000;

        #region Internals
        private bool _isCancelable;
        private bool _isCanceled;
        private int _progressPercentage;
        private string _progressState;
        private string _taskName;
        private Visibility _visibility = Visibility.Collapsed;

        private readonly Mutex _mutex = new Mutex();

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

        protected internal string[] ProgressingTaskMethodName => GetType().ToQualifiedMethodName(nameof(ConsumeAstProgressingTask));

        [JsonIgnore]
        internal LogReason LogReasonMask { get; set; } = LogReason.Any;

        internal void OnTaskProgessEnded(object sender, NodeProgressEndEventArgs? args = null)
        {
            if (LogReason.None != (LogReasonMask & args?.Reason))
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
                if (_mutex.WaitOne(TimeoutMilliseconds))
                {
                    ProgressPercentage = args.Percentage;
                    ProgressState = args.State?.ToString() ?? args.TaskName;
                    TaskName = args.TaskName;

                    if (null != args?.InternalState)
                    {
                        if (_isCanceled && !args.InternalState.IsAboutCancelling)
                            args.InternalState.MarkCancelling();
                    }

                    if (sender is ProgressingTask task)
                    {
                        if (_isCanceled)
                            task.CancelAll();
                    }
                    
                    _mutex.ReleaseMutex();
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
            _mutex.WaitOne();
            var tasks = ActiveTasks.ToArray();
            _mutex.ReleaseMutex();
            return tasks;
        }

        public void ClearActiveTaskList()
        {
            DispatchOnUIThread(() =>
            {
                _mutex.WaitOne();
                ActiveTasks.Clear();
                _mutex.ReleaseMutex();
            });
        }

        public ProgressingTaskInfo FindActiveTaskInfo(ProgressingTask task)
        {
            _mutex.WaitOne();
            var taskInfo = ActiveTasks.FirstOrDefault(taskInfo => ReferenceEquals(taskInfo.Task, task));
            _mutex.ReleaseMutex();
            return taskInfo;
        }

        public void DispatchCreateOrUpdate(ProgressingTask task)
        {
            DispatchOnUIThread(() =>
            {
                if (TryCreateActiveTaskInfo(task, out var taskInfo))
                {
                    taskInfo.Update();
                }
            });
        }

        public bool TryCreateActiveTaskInfo(ProgressingTask task, out ProgressingTaskInfo taskInfo)
        {
            taskInfo = null;
            if (_mutex.WaitOne(TimeoutMilliseconds))
            {
                taskInfo = ActiveTasks.FirstOrDefault(taskInfo => ReferenceEquals(taskInfo.Task, task));
                if (null == taskInfo)
                {
                    taskInfo = new ProgressingTaskInfo(task);
                    ActiveTasks.Add(taskInfo);
                }
                _mutex.ReleaseMutex();
            }     
            return taskInfo != null;
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
                _mutex.WaitOne();
                var v = _isCanceled;
                _mutex.ReleaseMutex();
                return v;
            }
            set
            {
                _mutex.WaitOne();
                _isCanceled = value;
                _mutex.ReleaseMutex();

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
            _mutex.WaitOne();
            
            ProgressPercentage = 0;
            ProgressState = DefaultProgressState;
            TaskName = DefaultTaskName;
            
            _mutex.ReleaseMutex();
            
            CancellationVisibility = Visibility.Collapsed;
        }

        public void Report(int percentage, object userState)
        {
            _mutex.WaitOne();
            
            ProgressPercentage = percentage;
            ProgressState = $"{userState?.ToString() ?? "Running"}";
            
            _mutex.ReleaseMutex();
        }

        public void Report(ProgressStateToken value)
        {
            _mutex.WaitOne();
            
            var percentage = value.Percentage;
            ProgressPercentage = percentage;
            ProgressState = $"{percentage}%";

            if (IsCancelable && IsCanceled)
            {
                value.MarkCanceled();
            }
            
            _mutex.ReleaseMutex();
        }
    }
}

#pragma warning restore CS1591
