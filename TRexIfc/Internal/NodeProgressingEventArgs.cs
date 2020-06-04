using System;

using Bitub.Transfer;

using Autodesk.DesignScript.Runtime;
using Log;

namespace Internal
{
    /// <summary>
    /// Node progressing event arguments.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public class NodeProgressingEventArgs : EventArgs
    {
        /// <summary>
        /// Internal state object
        /// </summary>
        public readonly ICancelableProgressState InternalState;

        /// <summary>
        /// The state.
        /// </summary>
        public readonly object State;

        /// <summary>
        /// The task name.
        /// </summary>
        public readonly string TaskName;
        
        /// <summary>
        /// The percentage (between 0 and 100)
        /// </summary>
        public readonly int Percentage;

        /// <summary>
        /// The action type.
        /// </summary>
        public readonly LogReason Action;

        /// <summary>
        /// Wrapping an internal event reference.
        /// </summary>
        /// <param name="state">The source state</param>
        /// <param name="taskName">The task name</param>
        /// <param name="action">The action type</param>
        public NodeProgressingEventArgs(LogReason action, ICancelableProgressState state, string taskName = null)
        {
            InternalState = state;
            TaskName = $"{taskName ?? state.State.ToString()}";
            State = state.StateObject;
            Percentage = state.Percentage;
            Action = action;
        }

        /// <summary>
        /// Explicitely instanciation of event argument.
        /// </summary>
        /// <param name="percentage">The percetange</param>
        /// <param name="taskName">The task name</param>
        /// <param name="state">The state</param>
        /// <param name="action">The action type</param>
        public NodeProgressingEventArgs(LogReason action, int percentage, string taskName = null, object state = null)
        {
            Percentage = Math.Max(0, Math.Min(100, percentage));
            TaskName = taskName;
            State = state;
            Action = action;
        }
    }
}

