using System;

using Bitub.Transfer;

using Log;

using System.Runtime.CompilerServices;
using Autodesk.DesignScript.Runtime;

[assembly: InternalsVisibleTo("TRexIfcUI")]

namespace Internal
{
    /// <summary>
    /// Node finish action event arguments.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public class NodeProgressEndEventArgs : NodeProgressEventArgs
    {       
        /// <summary>
        /// Whether canceled by user.
        /// </summary>
        internal readonly bool IsCanceled;

        /// <summary>
        /// Whether broken by internals.
        /// </summary>
        internal readonly bool IsBroken;

        /// <summary>
        /// New progress end by internal state and task name.
        /// </summary>
        /// <param name="endState">The internal state</param>
        /// <param name="taskName">The task name</param>
        /// <param name="reason">The log reason type</param>
        internal NodeProgressEndEventArgs(LogReason reason, ProgressStateToken endState, string taskName = null)
            : base(reason, endState, taskName)
        {
            IsCanceled = endState?.State.HasFlag(ProgressTokenState.IsCanceled) ?? false;
            IsBroken = endState?.State.HasFlag(ProgressTokenState.IsBroken) ?? false;
        }

        /// <summary>
        /// New progress end by giving explicit details.
        /// </summary>
        /// <param name="taskName">Taskname</param>
        /// <param name="isCanceled">Cancellation flag</param>
        /// <param name="isBroken">Broken flag</param>
        /// <param name="reason">The action type</param>
        /// <param name="stateObject">The end state object</param>
        internal NodeProgressEndEventArgs(LogReason reason, string taskName, object stateObject = null, bool isCanceled = false, bool isBroken = false)
            : base(reason, 100, taskName, stateObject)
        {
            IsCanceled = isCanceled;
            IsBroken = isBroken;
        }
    }
}

