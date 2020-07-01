using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Autodesk.DesignScript.Runtime;
using Bitub.Transfer;
using Log;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("TRexIfcUI")]

namespace Internal
{
    /// <summary>
    /// Node progressing event template
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public abstract class NodeProgressing : IProgress<ICancelableProgressState>
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
        internal ObservableCollection<LogMessage> ActionLog { get; } = new ObservableCollection<LogMessage>();

        /// <summary>
        /// Retrieves the current log state as an array.
        /// </summary>
        /// <param name="nodeProgressing">The logging source</param>
        /// <returns>The current log</returns>
        [IsVisibleInDynamoLibrary(false)]
        internal static LogMessage[] GetActionLog(NodeProgressing nodeProgressing) => nodeProgressing?.ActionLog.ToArray();

        [IsVisibleInDynamoLibrary(false)]
        internal event EventHandler<NodeProgressingEventArgs> OnProgressChange
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

        [IsVisibleInDynamoLibrary(false)]
        internal event EventHandler<NodeFinishedEventArgs> OnFinish
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
        internal virtual void ClearState()
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
        internal virtual void OnProgressChanged(NodeProgressingEventArgs args)
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
        internal virtual void OnFinished(NodeFinishedEventArgs args)
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
        internal void NotifyFinish(LogReason finishAction, bool isBroken)
        {
            OnFinished(new NodeFinishedEventArgs(finishAction, Name, false, isBroken));
        }

        /// <summary>
        /// The name.
        /// </summary>
        internal virtual string Name { get => "Progressing node"; }

        /// <summary>
        /// Default log reason.
        /// </summary>
        protected virtual LogReason DefaultReason { get => LogReason.Changed; }

        /// <summary>
        /// Reporting progress from outside.
        /// </summary>
        /// <param name="value">The progress state</param>
        [IsVisibleInDynamoLibrary(false)]
        public void Report(ICancelableProgressState value)
        {
            OnProgressChanged(new NodeProgressingEventArgs(DefaultReason, value));
        }
    }
}

