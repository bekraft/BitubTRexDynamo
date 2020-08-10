using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Autodesk.DesignScript.Runtime;
using Bitub.Transfer;
using Log;

using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using Autodesk.DesignScript.Geometry;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("TRexIfcUI")]

namespace Internal
{
    /// <summary>
    /// Node progressing event template
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public abstract class NodeProgressing : IProgress<ProgressStateToken>
    {
        #region Internals
        private readonly object _monitor = new object();

        private EventHandler<NodeProgressEventArgs> _onProgressChangeEvent;
        private EventHandler<NodeProgressEndEventArgs> _onProgressEndEvent;
        
        private NodeProgressEventArgs _progressEventArgs;
        private NodeProgressEndEventArgs _progressEndEventArgs;

        private ConcurrentDictionary<ProgressStateToken, CancelableProgressing> _openProgress = new ConcurrentDictionary<ProgressStateToken, CancelableProgressing>();

        private readonly static ILogger<NodeProgressing> Log = GlobalLogging.LoggingFactory.CreateLogger<NodeProgressing>();
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
        internal static LogMessage[] GetActionLog(NodeProgressing nodeProgressing) => nodeProgressing?.ActionLog.ToArray();

        internal event EventHandler<NodeProgressEventArgs> OnProgressChange
        {
            add {
                NodeProgressEventArgs args;
                lock (_monitor)
                {
                    _onProgressChangeEvent += value;
                    args = _progressEventArgs;
                }

                if (null != args)
                    value.Invoke(this, args);
            }
            remove {
                lock (_monitor)
                    _onProgressChangeEvent -= value;
            }
        }

        internal event EventHandler<NodeProgressEndEventArgs> OnProgressEnd
        {
            add {
                NodeProgressEndEventArgs args;
                lock (_monitor)
                {
                    _onProgressEndEvent += value;
                    args = _progressEndEventArgs;
                }

                if (null != args)
                    value.Invoke(this, _progressEndEventArgs);
            }
            remove {
                lock (_monitor)
                    _onProgressEndEvent -= value;
            }
        }

        /// <summary>
        /// Clears the current known state.
        /// </summary>
        internal virtual void ClearState()
        {
            lock (_monitor)
            {
                _progressEndEventArgs = null;
                _progressEventArgs = null;
            }
        }

        /// <summary>
        /// Emitting progress changes
        /// </summary>
        /// <param name="args">The args</param>
        internal virtual void OnProgressChanged(NodeProgressEventArgs args)
        {
            EventHandler<NodeProgressEventArgs> eventHandler;
            lock (_monitor)
            {
                eventHandler = _onProgressChangeEvent;
                _progressEventArgs = args;
            }

            eventHandler?.Invoke(this, args);
        }

        /// <summary>
        /// Emitting finish actions
        /// </summary>
        /// <param name="args">The args</param>
        internal virtual void OnProgressEnded(NodeProgressEndEventArgs args)
        {
            EventHandler<NodeProgressEndEventArgs> eventHandler;
            lock (_monitor)
            {
                eventHandler = _onProgressEndEvent;
                _progressEndEventArgs = args;

                CancelableProgressing cp;
                Log.LogInformation($"Detected progress end with for '{args.TaskName}' with logging filter '{args.Action}'");
                if ((null != args.InternalState) && _openProgress.TryRemove(args.InternalState, out cp))
                {
                    cp.Dispose();
                    Log.LogInformation($"Progress monitor ended with {cp.State.State} at {cp.State.Percentage}%");
                }
            }

            eventHandler?.Invoke(this, args);
        }

        /// <summary>
        /// Manually notifying on finish.
        /// </summary>
        /// <param name="finishAction">The finishing action.</param>
        /// <param name="isBroken">Whether the finish is reached by error</param>
        internal void OnProgressEnded(LogReason finishAction, bool isBroken)
        {
            OnProgressEnded(new NodeProgressEndEventArgs(finishAction, Name, false, isBroken));
        }

        /// <summary>
        /// The name.
        /// </summary>
        internal virtual string Name { get => "Progressing node"; }

        /// <summary>
        /// Returns an array of open progresses.
        /// </summary>
        /// <returns>Open progressing</returns>
        internal CancelableProgressing[] GetOpenProgresses()
        {
            return _openProgress.Values.ToArray();
        }

        internal bool IsBusy 
        { 
            get => _openProgress.Count > 0; 
        }

        internal void CancelAll()
        {
            GetOpenProgresses().ForEach(p => p.Cancel());
        }

        /// <summary>
        /// Gets or creates a new progress monitor.
        /// </summary>
        /// <returns>A cancelable progress monitor reporting to this node model</returns>
        internal CancelableProgressing CreateProgressMonitor(LogReason logReason)
        {
            var cp = new CancelableProgressing(true);
            if (!_openProgress.TryAdd(cp.State, cp))
                throw new NotSupportedException("Internal state exception. Progress token already added.");

            // Attach forwarding of events
            cp.OnProgressChange += (sender, e) => OnProgressChanged(new NodeProgressEventArgs(logReason, e));
            cp.OnProgressEnd += (sender, e) => OnProgressEnded(new NodeProgressEndEventArgs(logReason, e));

            Log.LogInformation($"Progress monitor started with '{cp.State.State}' at {cp.State.Percentage} % logging for '{logReason}'");

            return cp;
        }

        /// <summary>
        /// Default log reason.
        /// </summary>
        protected virtual LogReason DefaultReason { get => LogReason.Changed; }

        /// <summary>
        /// Reporting progress from outside.
        /// </summary>
        /// <param name="value">The progress state</param>
        [IsVisibleInDynamoLibrary(false)]
        public void Report(ProgressStateToken value)
        {
            OnProgressChanged(new NodeProgressEventArgs(DefaultReason, value));
        }
    }
}

