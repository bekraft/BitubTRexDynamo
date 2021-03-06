﻿using System;
using System.Collections.Generic;
using System.Windows;
using Dynamo.Graph.Nodes;

using Newtonsoft.Json;

using Bitub.Transfer;
using Autodesk.DesignScript.Runtime;
using Internal;

// Disable comment warning
#pragma warning disable CS1591

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

        private ICancelableProgressState _progressToken;

        private object _monitor = new object();
        #endregion

        protected CancelableProgressingNodeModel() : base()
        {
            ResetState();
        }

        protected CancelableProgressingNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            ResetState();
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
                _progressPercentage = Math.Max(0, Math.Min(100, value));
                RaisePropertyChanged(nameof(ProgressPercentage));
            }
        }

        public void InitNode(ICancelableProgressState progressToken)
        {
            lock (_monitor)
            {
                _progressToken = progressToken;

                if (IsCancelable && IsCanceled)
                    _progressToken.MarkCanceled();
            }
        }

        public void ResetState()
        {
            lock (_monitor)
            {
                ProgressPercentage = 0;
                ProgressState = "ready";
                TaskName = "(not active)";
            }
        }

        public void ClearState()
        {
            lock (_monitor)
            {
                ProgressState = "done";
                TaskName = "(Finished)";
            }    
        }

        public void Report(int percentage, object userState)
        {
            lock(_monitor)
            {
                ProgressPercentage = percentage;
                ProgressState = $"{userState?.ToString() ?? "Running"}";
            }
        }

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
