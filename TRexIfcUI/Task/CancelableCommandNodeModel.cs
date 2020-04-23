using System;
using System.Collections.Generic;
using System.Windows;
using Dynamo.Graph.Nodes;

using Newtonsoft.Json;

using Bitub.Transfer;
using Autodesk.DesignScript.Runtime;

// Disable comment warning
#pragma warning disable CS1591

namespace Task
{
    public abstract class CancelableCommandNodeModel : NodeModel, ICancelableTaskNode
    {
        #region Internals
        private bool _isCancelable;
        private bool _isCanceled;
        private int _progressPercentage;
        private string _progressState;
        private string _taskName;
        private Visibility _visibility;

        private ICancelableProgressState _progressToken;

        private object _monitor = new object();
        #endregion

        protected CancelableCommandNodeModel() : base()
        {
            ResetState();
        }

        protected CancelableCommandNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            ResetState();
        }        

        [JsonIgnore]
        public Visibility CancellationVisibility
        {
            get => _visibility;
            set {
                _visibility = value;
                RaisePropertyChanged(nameof(CancellationVisibility));
            }
        }

        public bool IsCancelable
        {
            get => _isCancelable;
            set {
                _isCancelable = value;
                RaisePropertyChanged(nameof(IsCancelable));
            }
        }

        [JsonIgnore]
        public bool IsCanceled
        {
            get => _isCanceled;
            set {
                _isCanceled = value;
                RaisePropertyChanged(nameof(IsCanceled));
            }
        }

        [JsonIgnore]
        public string ProgressState
        {
            get => _progressState;
            set {
                _progressState = value;
                RaisePropertyChanged(nameof(ProgressState));                
            }
        }

        [JsonIgnore]
        public string TaskName
        {
            get => _taskName;
            set {
                _taskName = value;
                RaisePropertyChanged(nameof(TaskName));                
            }
        }

        [JsonIgnore]
        public int ProgressPercentage
        {
            get => _progressPercentage;
            set {
                _progressPercentage = Math.Max(0, Math.Min(100, value));
                RaisePropertyChanged(nameof(ProgressPercentage));
            }
        }

        [IsVisibleInDynamoLibrary(false)]
        public void InitNode(ICancelableProgressState progressToken)
        {
            lock (_monitor)
            {
                _progressToken = progressToken;

                if (IsCancelable && IsCanceled)
                    _progressToken.MarkCanceled();
            }
        }

        [IsVisibleInDynamoLibrary(false)]
        public void ResetState()
        {
            lock (_monitor)
            {
                ProgressPercentage = 0;
                ProgressState = "ready";
                TaskName = "";
            }
        }


        [IsVisibleInDynamoLibrary(false)]
        public void ClearState()
        {
            lock (_monitor)
            {
                ProgressState = "done";
                TaskName = "";
            }    
        }

        [IsVisibleInDynamoLibrary(false)]
        public void Report(int percentage, object userState)
        {
            lock(_monitor)
            {
                ProgressPercentage = percentage;
                ProgressState = $"{userState?.ToString() ?? "Running"}";
            }
        }

        [IsVisibleInDynamoLibrary(false)]
        public void Report(ICancelableProgressState value)
        {
            lock (_monitor)
            {
                _progressToken = value;

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
