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
        ICollection<LogMessage> LogMessages { get; }
    }

    /// <summary>
    /// Node progressing event template
    /// </summary>    
    public abstract class NodeProgressing : INodeProgressing
    {
        #region Internals
        private EventHandler<NodeProgressingEventArgs> _onProgressChangeEvent;
        private EventHandler<NodeFinishedEventArgs> _onFinishEvent;
        private object _monitor = new object();
        private NodeProgressingEventArgs _recentProgressEventArgs;
        private NodeFinishedEventArgs _recentFinishEventArgs;
        #endregion

        /// <summary>
        /// Logging messages.
        /// </summary>
        public ICollection<LogMessage> LogMessages { get; } = new ObservableCollection<LogMessage>();

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
        /// Reporting progress from outside.
        /// </summary>
        /// <param name="value">The progress state</param>
        [IsVisibleInDynamoLibrary(false)]
        public void Report(ICancelableProgressState value)
        {
            OnProgressChanged(new NodeProgressingEventArgs(ActionType.Change, value));
        }
    }
}

