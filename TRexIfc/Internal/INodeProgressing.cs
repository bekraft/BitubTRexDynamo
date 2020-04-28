using System;

using Bitub.Transfer;

using Autodesk.DesignScript.Runtime;

namespace Internal
{
    /// <summary>
    /// Interface tagging an interactive zero touch node.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public interface INodeProgressing
    {
        /// <summary>
        /// Emitting progress change information
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        event EventHandler<NodeProgressingEventArgs> OnProgressChange;

        /// <summary>
        /// Emitting once progress has been finished.S
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        event EventHandler<NodeFinishedEventArgs> OnFinish;

        /// <summary>
        /// Mark all progressing actions to be canceled.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        void MarkAllCanceled();
    }

    /// <summary>
    /// Node finish action event arguments.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public class NodeFinishedEventArgs : EventArgs
    {
        /// <summary>
        /// The associated task name.
        /// </summary>
        public readonly string TaskName;

        /// <summary>
        /// Whether canceled by user.
        /// </summary>
        public readonly bool IsCanceled;

        /// <summary>
        /// Whether broken by internals.
        /// </summary>
        public readonly bool IsBroken;

        /// <summary>
        /// Reference to internal state.
        /// </summary>
        public readonly IProgressState InternalFinalState;

        /// <summary>
        /// New finish by internal state and task name
        /// </summary>
        /// <param name="finalState">The internal state</param>
        /// <param name="taskName">The task name</param>
        public NodeFinishedEventArgs(IProgressState finalState, string taskName = null)
        {
            TaskName = taskName ?? $"{finalState.StateObject}";
            IsCanceled = finalState.State == ProgressTokenState.IsCanceled;
        }

        /// <summary>
        /// New finish by giving explicit details
        /// </summary>
        /// <param name="taskName">Taskname</param>
        /// <param name="isCanceled">Cancellation flag</param>
        /// <param name="isBroken">Broken flag</param>
        public NodeFinishedEventArgs(string taskName, bool isCanceled = false, bool isBroken = false)
        {
            TaskName = taskName;
            IsCanceled = isCanceled;
            IsBroken = isBroken;
        }
    }

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
        /// Wrapping an internal event reference.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="taskName"></param>
        public NodeProgressingEventArgs(ICancelableProgressState state, string taskName = null)
        {
            InternalState = state;
            TaskName = $"{taskName ?? state.StateObject}";
            State = state.StateObject;
            Percentage = state.Percentage;
        }

        /// <summary>
        /// Explicitely instanciation of event argument.
        /// </summary>
        /// <param name="percentage">The percetange</param>
        /// <param name="taskName">The task name</param>
        /// <param name="state">The state</param>
        public NodeProgressingEventArgs(int percentage, string taskName = null, string state = null)
        {
            Percentage = Math.Max(0, Math.Min(100, percentage));
            TaskName = taskName;
            State = state;
        }
    }
}

