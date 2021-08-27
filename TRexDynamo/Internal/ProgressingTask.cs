using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Bitub.Dto;
using TRex.Log;

using Autodesk.DesignScript.Runtime;

using Microsoft.Extensions.Logging;

namespace TRex.Internal
{
#pragma warning disable CS1591

    /// <summary>
    /// A progressing task which emits changes, progress end events and logging data.
    /// </summary>
    public abstract class ProgressingTask
    {
        #region Internals

        #region Thread safety

        private readonly object _mutex = new object();

        private NodeProgressEventArgs __eventArgs;
        private EventHandler<NodeProgressEventArgs> __onProgressChangeEvent;
        private EventHandler<NodeProgressEndEventArgs> __onProgressEndEvent;
        private EventHandler<IEnumerable<LogMessage>> __onLogActionEvent;

        #endregion

        // Logged messages
        private ConcurrentQueue<LogMessage> actionLog;
        // Progress states vs monitor
        private ConcurrentDictionary<ProgressStateToken, CancelableProgressing> progressMonitor;
        
        /// <summary>
        /// The logger reference.
        /// </summary>
        protected internal ILogger Log { get; protected set; }

        protected internal ProgressingTask(LoggerFactory loggerFactory = null)
        {
            this.actionLog = new ConcurrentQueue<LogMessage>();
            this.progressMonitor = new ConcurrentDictionary<ProgressStateToken, CancelableProgressing>();

            Log = loggerFactory?.CreateLogger(GetType());
        }

        protected internal void ShareActionLog(ProgressingTask otherProgressingTask)
        {
            lock (otherProgressingTask._mutex)
                otherProgressingTask.actionLog = this.actionLog;
        }
        
        #region Event handling

        internal event EventHandler<NodeProgressEventArgs> OnProgressChange
        {
            add {
                lock (_mutex)
                {
                    if (!__onProgressChangeEvent?.GetInvocationList().Contains(value) ?? true)
                        __onProgressChangeEvent += value;
                }
            }
            remove {
                lock (_mutex)
                    __onProgressChangeEvent -= value;
            }
        }

        internal event EventHandler<IEnumerable<LogMessage>> OnLogAction
        {
            add
            {
                lock (_mutex)
                {
                    if (!__onLogActionEvent?.GetInvocationList().Contains(value) ?? true)
                        __onLogActionEvent += value;
                }
            }
            remove
            {
                lock (_mutex)
                    __onLogActionEvent -= value;
            }
        }

        internal event EventHandler<NodeProgressEndEventArgs> OnProgressEnd
        {
            add {
                lock (_mutex)
                {
                    if (!__onProgressEndEvent?.GetInvocationList().Contains(value) ?? true)
                        __onProgressEndEvent += value;
                }
            }
            remove {
                lock (_mutex)
                    __onProgressEndEvent -= value;
            }
        }

        internal NodeProgressEventArgs LatestProgressEventArgs
        {
            get {
                lock (_mutex)
                    return __eventArgs;
            }
        }

        internal void OnActionLogged(params LogMessage[] messages)
        {
            EventHandler<IEnumerable<LogMessage>> eventHandler;
            lock (_mutex)
            {
                eventHandler = __onLogActionEvent;
            }
            messages.ForEach(m =>
            {
                m.PropagateToLog(Log);
                actionLog.Enqueue(m);
            });
            eventHandler?.Invoke(this, messages);
        }

        internal void OnProgressChanged(NodeProgressEventArgs args)
        {
            EventHandler<NodeProgressEventArgs> eventHandler;
            lock (_mutex)
            {
                eventHandler = __onProgressChangeEvent;
                __eventArgs = args;
            }

            eventHandler?.Invoke(this, args);
        }

        internal void OnProgressEnded(NodeProgressEndEventArgs args)
        {
            EventHandler<NodeProgressEndEventArgs> eventHandler;
            lock (_mutex)
            {
                eventHandler = __onProgressEndEvent;
                __eventArgs = args;

                CancelableProgressing cp;
                Log?.LogInformation($"Detected progress end for '{args.TaskName}' with logging filter '{args.Reason}'");
                if ((null != args.InternalState) && progressMonitor.TryRemove(args.InternalState, out cp))
                {                    
                    Log?.LogInformation($"Progress monitor ended with {cp.State.State} at {cp.State.Percentage}%");
                    cp.Dispose();
                }
            }

            eventHandler?.Invoke(this, args);
        }

        /// <summary>
        /// Manually notifying on finish.
        /// </summary>
        /// <param name="finishAction">The finishing action.</param>
        /// <param name="isBroken">Whether the finish is reached by error</param>
        internal protected void OnProgressEnded(LogReason finishAction, bool isBroken)
        {
            OnProgressEnded(new NodeProgressEndEventArgs(finishAction, Name, false, isBroken));
        }

        /// <summary>
        /// Notifies all sinks on progress change.
        /// </summary>
        /// <param name="action">The logging action happened</param>
        /// <param name="percentage">The current percentage</param>
        /// <param name="stateObject">The current state object</param>
        internal protected void NotifyOnProgressChanged(LogReason action, int percentage, object stateObject)
        {
            OnProgressChanged(new NodeProgressEventArgs(action, percentage, Name, stateObject));
        }

        /// <summary>
        /// Notifies all sinks on progress end.
        /// </summary>
        /// <param name="action">The logging action happened</param>
        /// <param name="isCanceled">Whether the progress was cancelled</param>
        /// <param name="isBroken">Whether the progress was broken</param>
        internal protected void NotifyOnProgressEnded(LogReason action, bool isCanceled, bool isBroken)
        {
            OnProgressEnded(new NodeProgressEndEventArgs(action, Name, isCanceled, isBroken));
        }

        #endregion

        /// <summary>
        /// The name.
        /// </summary>        
        internal virtual string Name { get; set; } = "Progressing node";

        /// <summary>
        /// Returns an array of open progresses.
        /// </summary>
        /// <returns>Open progressing</returns>
        internal CancelableProgressing[] GetOpenProgresses()
        {
            return progressMonitor.Values.ToArray();
        }

        internal bool IsBusy 
        { 
            get => progressMonitor.Count > 0; 
        }

        internal bool IsCanceled { get; private set; } = false;

        internal void CancelAll()
        {
            IsCanceled = true;
            GetOpenProgresses().ForEach(p => p.Cancel());            
        }

        /// <summary>
        /// Gets or creates a new progress monitor.
        /// </summary>
        /// <returns>A cancelable progress monitor reporting to this node model</returns>
        internal protected CancelableProgressing CreateProgressMonitor(LogReason logReason)
        {
            var cp = new CancelableProgressing(true);
            if (!progressMonitor.TryAdd(cp.State, cp))
                throw new NotSupportedException("Internal state exception. Progress token already added.");

            // Attach forwarding of events
            cp.OnProgressChange += (sender, e) => OnProgressChanged(new NodeProgressEventArgs(logReason, e, Name));
            cp.OnProgressEnd += (sender, e) => OnProgressEnded(new NodeProgressEndEventArgs(logReason, e, Name));

            Log?.LogInformation($"Progress monitor started with '{cp.State.State}' at {cp.State.Percentage} % logging for '{logReason}'");

            return cp;
        }

        /// <summary>
        /// Default log reason.
        /// </summary>
        protected virtual LogReason DefaultReason { get => LogReason.Changed; }

        #endregion

        /// <summary>
        /// Fetches the most recent logged actions from log.
        /// </summary>
        /// <returns>A sequence of logged actions</returns>
        [IsVisibleInDynamoLibrary(false)]
        public IEnumerable<LogMessage> FetchRecentLogActions()
        {
            LogMessage message;
            while (actionLog.TryDequeue(out message))
                yield return message;
        }

        /// <summary>
        /// Retrieve current state of action log.
        /// </summary>
        /// <param name="nodeProgressing">The progressing node</param>
        /// <returns>An array of recent messages</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static LogMessage[] GetActionLog(ProgressingTask nodeProgressing)
        {
            return nodeProgressing?.GetActionLog();
        }

        /// <summary>
        /// Retrieves the current state of action log.
        /// </summary>
        /// <returns>An array of recent messages</returns>
        [IsVisibleInDynamoLibrary(false)]
        public LogMessage[] GetActionLog()
        {
            return actionLog.ToArray();
        }

        public override string ToString()
        {
            return string.Format("{0} '{1}' ({2})", 
                GetType().Name, Name, LatestProgressEventArgs?.GetProgressState().ToString() ?? "(unknown state)");
        }

#pragma warning restore CS1591
    }
}

