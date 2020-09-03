﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;

using Bitub.Transfer;
using Internal;

namespace UI
{
    /// <summary>
    /// Simple information element for visual representation
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public class ProgressingTaskInfo : INotifyPropertyChanged
    {
        #region Internals
        #endregion

        /// <summary>
        /// Property change event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        internal ProgressingTaskInfo(ProgressingTask task)
        {
            Task = task;
            ElapsedSpan = GlobalLogging.DiagnosticStopWatch.Elapsed;
            Name = task.Name;
            SetStatus(task);
        }

        private void SetStatus(ProgressingTask task)
        {
            if (null != task.LatestProgressEventArgs)
                Status = task.LatestProgressEventArgs.GetProgressState() & (ProgressTokenState)0xf0;
            else
                Status = ProgressTokenState.IsTerminated;
        }

        internal void Update()
        {
            Name = Task.Name;
            SetStatus(Task);
            NotifyPropertyChanged(nameof(Name), nameof(Status));
        }

        internal void NotifyPropertyChanged(params string[] property)
        {
            property.ForEach((p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p)));
        }

        internal ProgressingTask Task { get; private set; }

        /// <summary>
        /// Time span spent so far.
        /// </summary>
        public TimeSpan ElapsedSpan { get; private set; }

        /// <summary>
        /// Name of task.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Latest progress state.
        /// </summary>
        public ProgressTokenState? Status { get; private set; }

        /// <summary>
        /// Referes to the equality of instance references of tasks.
        /// </summary>
        /// <param name="obj">Some task</param>
        /// <returns>True, if both infos refer to the same task</returns>
        public override bool Equals(object obj)
        {
            if (obj is ProgressingTaskInfo info)
                return object.ReferenceEquals(Task, info.Task);
            else
                return false;
        }
    }
}
