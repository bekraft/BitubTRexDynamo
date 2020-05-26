using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Autodesk.DesignScript.Runtime;
using Bitub.Transfer;
using Log;

namespace Internal
{
    /// <summary>
    /// Interface tagging an interactive zero touch node.
    /// </summary>    
    public interface INodeProgressing : IProgress<ICancelableProgressState>
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
        /// Log message receiver.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        ICollection<LogMessage> ActionLog { get; }

        /// <summary>
        /// A representative name.
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// Node progressing event template
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public abstract class NodeProgressing : INodeProgressing
    {
        #region Internals
        private EventHandler<NodeProgressingEventArgs> _onProgressChangeEvent;
        private EventHandler<NodeFinishedEventArgs> _onFinishEvent;
        private readonly object _monitor = new object();
        private NodeProgressingEventArgs _recentProgressEventArgs;
        private NodeFinishedEventArgs _recentFinishEventArgs;
        #endregion

        /// <summary>
        /// Logging messages.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public ICollection<LogMessage> ActionLog { get; } = new ObservableCollection<LogMessage>();

        /// <summary>
        /// See <see cref="INodeProgressing.OnProgressChange"/>
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public event EventHandler<NodeProgressingEventArgs> OnProgressChange
        {
            add {
                NodeProgressingEventArgs args;
                lock (_monitor)
                {
                    _onProgressChangeEvent += value;
                    args = _recentProgressEventArgs;
                }

                if (null != args)
                    value.Invoke(this, args);
            }
            remove {
                lock (_monitor)
                    _onProgressChangeEvent -= value;
            }
        }

        /// <summary>
        /// See <see cref="INodeProgressing.OnFinish"/>
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public event EventHandler<NodeFinishedEventArgs> OnFinish
        {
            add {
                NodeFinishedEventArgs args;
                lock (_monitor)
                {
                    _onFinishEvent += value;
                    args = _recentFinishEventArgs;
                }

                if (null != args)
                    value.Invoke(this, _recentFinishEventArgs);
            }
            remove {
                lock (_monitor)
                    _onFinishEvent -= value;
            }
        }

        /// <summary>
        /// Clears the current known state.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public virtual void ClearState()
        {
            lock (_monitor)
            {
                _recentFinishEventArgs = null;
                _recentProgressEventArgs = null;
            }
        }

        /// <summary>
        /// Emitting progress changes
        /// </summary>
        /// <param name="args">The args</param>
        protected virtual void OnProgressChanged(NodeProgressingEventArgs args)
        {
            EventHandler<NodeProgressingEventArgs> onProgressEvent;
            lock (_monitor)
            {
                onProgressEvent = _onProgressChangeEvent;
                _recentProgressEventArgs = args;
            }

            onProgressEvent?.Invoke(this, args);
        }

        /// <summary>
        /// Emitting finish actions
        /// </summary>
        /// <param name="args">The args</param>
        protected virtual void OnFinished(NodeFinishedEventArgs args)
        {
            EventHandler<NodeFinishedEventArgs> onFinishEvent;
            lock (_monitor)
            {
                onFinishEvent = _onFinishEvent;
                _recentFinishEventArgs = args;
            }

            onFinishEvent?.Invoke(this, args);
        }

        /// <summary>
        /// Manually notifying on finish.
        /// </summary>
        /// <param name="finishAction">The finishing action.</param>
        /// <param name="isBroken">Whether the finish is reached by error</param>
        [IsVisibleInDynamoLibrary(false)]
        public void NotifyFinish(ActionType finishAction, bool isBroken)
        {
            OnFinished(new NodeFinishedEventArgs(finishAction, Name, false, isBroken));
        }

        /// <summary>
        /// The name.
        /// </summary>
        public virtual string Name { get => "Progressing node"; }

        /// <summary>
        /// Reporting progress from outside.
        /// </summary>
        /// <param name="value">The progress state</param>
        [IsVisibleInDynamoLibrary(false)]
        public void Report(ICancelableProgressState value)
        {
            OnProgressChanged(new NodeProgressingEventArgs(ActionType.Changed, value));
        }
    }
}

