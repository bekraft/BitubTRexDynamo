using System;
using System.Collections.Generic;
using System.Windows;
using Dynamo.Graph.Nodes;

using Newtonsoft.Json;

using Bitub.Transfer;
using Autodesk.DesignScript.Runtime;

// Disable comment warning
#pragma warning disable CS1591

namespace TRexIfc
{
    public abstract class CancelableCommandNode : NodeModel, IProgress<ICancelableProgressState>
    {
        #region Internals
        private bool _isCancelable;
        private int _progressPercentage;
        private string _progressState;
        private string _taskName;
        private Visibility _visibility;
        #endregion

        protected CancelableCommandNode() : base()
        {
        }

        protected CancelableCommandNode(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
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

        [JsonIgnore]
        public bool IsCancelable
        {
            get => _isCancelable;
            set {
                _isCancelable = value;
                RaisePropertyChanged(nameof(IsCancelable));
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
        public void Report(ICancelableProgressState value)
        {
            var percentage = value.Percentage;
            ProgressPercentage = percentage;            
            ProgressState = $"{percentage}%";
        }        
    }
}

#pragma warning restore CS1591
