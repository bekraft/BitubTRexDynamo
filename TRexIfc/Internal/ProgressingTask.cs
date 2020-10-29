using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Autodesk.DesignScript.Runtime;
using Bitub.Dto;
using Log;

using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using Autodesk.DesignScript.Geometry;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("TRexIfcUI")]

namespace Internal
{
    /// <summary>
    /// A progressing task which emits changes, progress end events and logging data.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public abstract class ProgressingTask : IDisposable
    {
        #region Internals
        private readonly object _mutex = new object();

        private NodeProgressEventArgs __eventArgs;
        private EventHandler<NodeProgressEventArgs> __onProgressChangeEvent;
        private EventHandler<NodeProgressEndEventArgs> __onProgressEndEvent;
        
        private ConcurrentDictionary<ProgressStateToken, CancelableProgressing> _progressMonitor = new ConcurrentDictionary<ProgressStateToken, CancelableProgressing>();

        private readonly static ILogger<ProgressingTask> Log = GlobalLogging.LoggingFactory.CreateLogger<ProgressingTask>();

        #endregion

        /// <summary>
        /// Logging messages.
        /// </summary>
        internal ObservableCollection<LogMessage> ActionLog { get; } = new ObservableCollection<LogMessage>();

        /// <summary>
        /// Retrieves the current log state as an array.
        /// </summary>
        /// <param name="nodeProgressing">The logging source</param>
        /// <returns>The current log</returns>
        internal static LogMessage[] GetActionLog(ProgressingTask nodeProgressing) => nodeProgressing?.ActionLog.ToArray();

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

        /// <summary>
        /// Emitting progress changes.
        /// </summary>
        /// <param name="args">The args</param>
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

        /// <summary>
        /// Emitting progress end.
        /// </summary>
        /// <param name="args">The args</param>
        internal void OnProgressEnded(NodeProgressEndEventArgs args)
        {
            EventHandler<NodeProgressEndEventArgs> eventHandler;
            lock (_mutex)
            {
                eventHandler = __onProgressEndEvent;
                __eventArgs = args;

                CancelableProgressing cp;
                Log.LogInformation($"Detected progress end with for '{args.TaskName}' with logging filter '{args.Reason}'");
                if ((null != args.InternalState) && _progressMonitor.TryRemove(args.InternalState, out cp))
                {                    
                    Log.LogInformation($"Progress monitor ended with {cp.State.State} at {cp.State.Percentage}%");
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
            return _progressMonitor.Values.ToArray();
        }

        internal bool IsBusy 
        { 
            get => _progressMonitor.Count > 0; 
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
        internal CancelableProgressing CreateProgressMonitor(LogReason logReason)
        {
            var cp = new CancelableProgressing(true);
            if (!_progressMonitor.TryAdd(cp.State, cp))
                throw new NotSupportedException("Internal state exception. Progress token already added.");

            // Attach forwarding of events
            cp.OnProgressChange += (sender, e) => OnProgressChanged(new NodeProgressEventArgs(logReason, e, Name));
            cp.OnProgressEnd += (sender, e) => OnProgressEnded(new NodeProgressEndEventArgs(logReason, e, Name));

            Log.LogInformation($"Progress monitor started with '{cp.State.State}' at {cp.State.Percentage} % logging for '{logReason}'");

            return cp;
        }

        /// <summary>
        /// Default log reason.
        /// </summary>
        protected virtual LogReason DefaultReason { get => LogReason.Changed; }

        /// <summary>
        /// Detaches all event handlers and clears current progress information.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public virtual void Dispose()
        {
            ActionLog.Clear();
            
            lock (_mutex)
            {
                __eventArgs = null;
                __onProgressChangeEvent = null;
                __onProgressEndEvent = null;
            }
        }

#pragma warning disable CS1591

        public override string ToString()
        {
            return string.Format("{0} '{1}' ({2})", 
                GetType().Name, Name, LatestProgressEventArgs?.GetProgressState().ToString() ?? "(unknown state)");
        }

#pragma warning restore CS1591
    }
}

