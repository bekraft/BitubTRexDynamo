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
        private readonly object _mutex = new object();

        private EventHandler<NodeProgressEventArgs> _onProgressChangeEvent;
        private EventHandler<NodeProgressEndEventArgs> _onProgressEndEvent;
        
        private ConcurrentDictionary<ProgressStateToken, CancelableProgressing> _progressMonitor = new ConcurrentDictionary<ProgressStateToken, CancelableProgressing>();

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
                lock (_mutex)
                {
                    if (!_onProgressChangeEvent.GetInvocationList().Contains(value))
                        _onProgressChangeEvent += value;
                }
            }
            remove {
                lock (_mutex)
                    _onProgressChangeEvent -= value;
            }
        }

        internal event EventHandler<NodeProgressEndEventArgs> OnProgressEnd
        {
            add {
                lock (_mutex)
                {
                    if (!_onProgressEndEvent.GetInvocationList().Contains(value))
                        _onProgressEndEvent += value;
                }
            }
            remove {
                lock (_mutex)
                    _onProgressEndEvent -= value;
            }
        }


        /// <summary>
        /// Emitting progress changes
        /// </summary>
        /// <param name="args">The args</param>
        internal virtual void OnProgressChanged(NodeProgressEventArgs args)
        {
            EventHandler<NodeProgressEventArgs> eventHandler;
            lock (_mutex)
                eventHandler = _onProgressChangeEvent;

            eventHandler?.Invoke(this, args);
        }

        /// <summary>
        /// Emitting finish actions
        /// </summary>
        /// <param name="args">The args</param>
        internal virtual void OnProgressEnded(NodeProgressEndEventArgs args)
        {
            EventHandler<NodeProgressEndEventArgs> eventHandler;
            lock (_mutex)
            {
                eventHandler = _onProgressEndEvent;

                CancelableProgressing cp;
                Log.LogInformation($"Detected progress end with for '{args.TaskName}' with logging filter '{args.Action}'");
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
            return _progressMonitor.Values.ToArray();
        }

        internal bool IsBusy 
        { 
            get => _progressMonitor.Count > 0; 
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
            if (!_progressMonitor.TryAdd(cp.State, cp))
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

