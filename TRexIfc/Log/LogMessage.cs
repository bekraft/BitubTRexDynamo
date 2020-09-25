using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.DesignScript.Runtime;
using System.Data;

using Internal;
using Microsoft.Extensions.Logging;

namespace Log
{
    /// <summary>
    /// A logging message of severity and message.
    /// </summary>
    public class LogMessage : IComparable<LogMessage>
    {
        #region Internals

        private static readonly ILogger Log = GlobalLogging.LoggingFactory.CreateLogger<LogMessage>();

        private readonly long _timeStamp;
        private string _messageTemplate;
        private object[] _args;

        internal LogMessage()
        {
            _timeStamp = DateTime.Now.ToBinary();
        }

        internal LogMessage(string source, LogSeverity severity, LogReason reason, string template, params object[] args) : this()
        {
            Source = source;
            Severity = severity;
            Reason = reason;
            _messageTemplate = template;
            _args = args;
        }

        #endregion

        /// <summary>
        /// The source.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// The severity.
        /// </summary>
        public LogSeverity Severity { get; private set; }

        /// <summary>
        /// The type of action.
        /// </summary>
        public LogReason Reason { get; private set; }

        /// <summary>
        /// The final message.
        /// </summary>
        public string Message { get => string.Format(_messageTemplate, _args); }

        /// <summary>
        /// The time stamp.
        /// </summary>
        public DateTime TimeStamp { get => DateTime.FromBinary(_timeStamp); }

        

        /// <summary>
        /// Splits the log message into severity, action and message.
        /// </summary>
        /// <param name="logMessages"></param>
        /// <returns></returns>
        [MultiReturn("severity", "action", "message")]
        public static Dictionary<string, object> ToValues(LogMessage[] logMessages)
        {
            return new Dictionary<string, object>() {
                { "severity", logMessages.Select(m => m.Severity.ToString()).ToArray() },
                { "reason", logMessages.Select(m => m.Reason.ToString()).ToArray() },
                { "message", logMessages.Select(m => m.Message).ToArray() },
            };
        }

        /// <summary>
        /// Returns a matrix of embedded log data.
        /// </summary>
        /// <param name="logMessage">The messages</param>        
        /// <returns>An array of log event data</returns>
        public static string[] ToData(LogMessage logMessage)
        {
            if (null == logMessage)
                return new string[] { };
            else
                return new string[] { logMessage.Severity.ToString(), logMessage.Reason.ToString(), logMessage.Message, logMessage.TimeStamp.ToString() };
        }

        /// <summary>
        /// Enables filtering by given severity.
        /// </summary>
        /// <param name="severity">The severity</param>
        /// <param name="messages">The message</param>
        /// <returns></returns>
        public static LogMessage[] FilterBySeverity(LogSeverity severity, LogMessage[] messages)
        {
            return messages.Where(m => SeverityExtensions.AboveOrEqual(m.Severity, severity)).ToArray();
        }

        /// <summary>
        /// Fetches the current action log.
        /// </summary>
        /// <param name="severity">Severity threshold</param>
        /// <param name="reasonFlagObj">Reason flag</param>
        /// <param name="nodeProgressing">Log source</param>
        /// <param name="maxLogCount">Max log count</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static LogMessage[] FilterBySeverity(LogSeverity severity, object reasonFlagObj, ProgressingTask nodeProgressing, int maxLogCount)
        {
            LogReason reasonFlag;
            if (!GlobalArgumentService.TryCastEnum(reasonFlagObj, out reasonFlag))
            {
                reasonFlag = LogReason.Any;
                Log.LogWarning($"Parsing reasonFlag failed in ({nameof(LogMessage.FilterBySeverity)}. Using '{reasonFlag}'.");                
            }

            return nodeProgressing?.ActionLog
                .Where(m => SeverityExtensions.AboveOrEqual(m.Severity, severity))
                .Where(m => (m.Reason & reasonFlag) != 0)
                .OrderBy(m => m)
                .Take(maxLogCount)
                .ToArray();
        }


        /// <summary>
        /// Groups by message types.
        /// </summary>
        /// <param name="messages">The log messages</param>
        /// <returns>An array of array of messages</returns>
        public static LogMessage[][] GroupByMessageType(LogMessage[] messages)
        {
            var groups = messages.GroupBy(m => m._messageTemplate).ToArray();
            return groups.Select(g => g.ToArray()).ToArray();
        }

        /// <summary>
        /// Groups by severity.
        /// </summary>
        /// <param name="messages">The messages</param>
        /// <returns>A dictionary of messages</returns>
        public static Dictionary<string, object> GroupBySeverity(LogMessage[] messages)
        {
            var groups = messages.GroupBy(m => m.Severity).OrderBy(g => g.Key).ToArray();
            return new Dictionary<string, object>()
            {
                { "severity", groups.Select(g => g.Key).ToArray() },
                { "messages", groups.ToDictionary(k => k.Key, v => v.ToArray()) }
            };
        }

        /// <summary>
        /// Groups by action type.
        /// </summary>
        /// <param name="messages">The messages</param>
        /// <returns>A dictionary of messages</returns>
        public static Dictionary<string, object> GroupByReason(LogMessage[] messages)
        {
            var groups = messages.GroupBy(m => m.Reason).ToArray();
            return new Dictionary<string, object>()
            {
                { "reason", groups.Select(g => g.Key).ToArray() },
                { "messages", groups.ToDictionary(k => k.Key, v => v.ToArray()) }
            };
        }

        /// <summary>
        /// Compares two messages by time stamp. Newer messages will rank before older.
        /// </summary>
        /// <param name="other">The other message</param>
        /// <returns>Rank indicator</returns>
        [IsVisibleInDynamoLibrary(false)]
        public int CompareTo(LogMessage other)
        {
            return -(int)(_timeStamp - other._timeStamp);
        }

#pragma warning disable CS1591

        [IsVisibleInDynamoLibrary(false)]
        public static LogMessage BySeverityAndMessage(string source, LogSeverity severity, LogReason reason, string messageTemplate, params object[] args)
        {
            return new LogMessage(source, severity, reason, messageTemplate, args);
        }

        
        public override string ToString()
        {
            return string.Format($"@'{Source}' {Severity} ({Reason}): {_messageTemplate}", _args);
        }

#pragma warning restore CS1591
    }
}
