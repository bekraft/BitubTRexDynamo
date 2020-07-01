using System;

using Bitub.Transfer;

using Autodesk.DesignScript.Runtime;
using Log;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("TRexIfcUI")]

namespace Internal
{
    /// <summary>
    /// Node progressing event arguments.
    /// </summary>
    internal class NodeProgressingEventArgs : EventArgs
    {
        /// <summary>
        /// Internal state object
        /// </summary>
        internal readonly ICancelableProgressState InternalState;

        /// <summary>
        /// The state.
        /// </summary>
        internal readonly object State;

        /// <summary>
        /// The task name.
        /// </summary>
        internal readonly string TaskName;
        
        /// <summary>
        /// The percentage (between 0 and 100)
        /// </summary>
        internal readonly int Percentage;

        /// <summary>
        /// The action type.
        /// </summary>
        internal readonly LogReason Reason;

        /// <summary>
        /// Wrapping an internal event reference.
        /// </summary>
        /// <param name="state">The source state</param>
        /// <param name="taskName">The task name</param>
        /// <param name="reason">The action type</param>
        internal NodeProgressingEventArgs(LogReason reason, ICancelableProgressState state, string taskName = null)
        {
            InternalState = state;
            TaskName = $"{taskName ?? state.State.ToString()}";
            State = state?.StateObject;
            Percentage = state?.Percentage ?? 0;
            Reason = reason;
        }

        /// <summary>
        /// Explicitely instanciation of event argument.
        /// </summary>
        /// <param name="percentage">The percetange</param>
        /// <param name="taskName">The task name</param>
        /// <param name="state">The state</param>
        /// <param name="reason">The action type</param>
        internal NodeProgressingEventArgs(LogReason reason, int percentage, string taskName = null, object state = null)
        {
            Percentage = Math.Max(0, Math.Min(100, percentage));
            TaskName = taskName;
            State = state;
            Reason = reason;
        }
    }
}

