using System;

using System.ComponentModel;

using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;

using Bitub.Dto;
using TRex.Internal;

namespace TRex.UI.Model
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

        private void SetStatus(ProgressingTask task, ProgressTokenState? defaultState = null)
        {
            if (null != task.LatestProgressEventArgs)
                Status = task.LatestProgressEventArgs.GetProgressState();
            else
                Status = defaultState ?? ProgressTokenState.IsRunning;
        }

        internal void Update(ProgressTokenState? defaultState = null)
        {
            Name = Task.Name;
            SetStatus(Task, defaultState);
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
                return ReferenceEquals(Task, info.Task);
            else
                return false;
        }

        /// <summary>
        /// Propagates the hash code of task.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Task.GetHashCode();
        }
    }
}
