using System;

using Bitub.Transfer;

using Autodesk.DesignScript.Runtime;
using Log;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("TRexIfcUI")]

namespace Internal
{
    /// <summary>
    /// Node finish action event arguments.
    /// </summary>
    internal class NodeFinishedEventArgs : EventArgs
    {
        /// <summary>
        /// The associated task name.
        /// </summary>
        internal readonly string TaskName;

        /// <summary>
        /// Whether canceled by user.
        /// </summary>
        internal readonly bool IsCanceled;

        /// <summary>
        /// Whether broken by internals.
        /// </summary>
        internal readonly bool IsBroken;

        /// <summary>
        /// The action type.
        /// </summary>
        internal readonly LogReason Action;

        /// <summary>
        /// Reference to internal state.
        /// </summary>
        internal readonly IProgressState InternalFinalState;

        /// <summary>
        /// New finish by internal state and task name
        /// </summary>
        /// <param name="finalState">The internal state</param>
        /// <param name="taskName">The task name</param>
        /// <param name="action">The action type</param>
        internal NodeFinishedEventArgs(IProgressState finalState, LogReason action, string taskName = null)
        {
            TaskName = taskName ?? $"{finalState?.StateObject}";
            IsCanceled = finalState?.State == ProgressTokenState.IsCanceled;
            Action = action;
        }

        /// <summary>
        /// New finish by giving explicit details
        /// </summary>
        /// <param name="taskName">Taskname</param>
        /// <param name="isCanceled">Cancellation flag</param>
        /// <param name="isBroken">Broken flag</param>
        /// <param name="action">The action type</param>
        internal NodeFinishedEventArgs(LogReason action, string taskName, bool isCanceled = false, bool isBroken = false)
        {
            TaskName = taskName;
            IsCanceled = isCanceled;
            IsBroken = isBroken;
            Action = action;
        }
    }
}

