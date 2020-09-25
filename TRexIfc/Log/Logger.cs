using System;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Core;

using Autodesk.DesignScript.Runtime;

namespace Log
{
    /// <summary>
    /// Logger instance capturing logging events to file.
    /// </summary>
    public class Logger
    {
        #region Internals

#pragma warning disable CS1591

        /// <summary>
        /// Logger factory
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public ILoggerFactory LoggerFactory { get; private set; }

        /// <summary>
        /// The default log.
        /// </summary>
        internal Serilog.ILogger DefaultLog { get; private set; }

        internal string MessageTemplate { get; set; } =         
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} ({ThreadId} '{ThreadName}'){NewLine}{Exception}";

        internal Logger()
        {
            LoggerFactory = new LoggerFactory();
        }

        [IsVisibleInDynamoLibrary(false)]
        public void LogInfo(string message, params object[] args)
        {
            DefaultLog.Information(message, args);
        }

        [IsVisibleInDynamoLibrary(false)]
        public void LogWarning(string message, params object[] args)
        {
            DefaultLog.Warning(message, args);
        }

        [IsVisibleInDynamoLibrary(false)]
        public void LogError(string message, params object[] args)
        {
            DefaultLog.Error(message, args);
        }

        [IsVisibleInDynamoLibrary(false)]
        public void LogError(Exception e, string message, params object[] args)
        {
            DefaultLog.Error(e, message, args);
        }

#pragma warning restore CS1591

        #endregion

        /// <summary>
        /// New logging instance.
        /// </summary>
        /// <param name="fileName">The file name to write to</param>
        /// <param name="levelSwitch">The minimum level switch</param>
        /// <returns>The bound logger instance</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static Logger ByLogFileName(string fileName, string levelSwitch = "Debug")
        {
            Serilog.Events.LogEventLevel level;
            try
            {
                level = (Serilog.Events.LogEventLevel)Enum.Parse(typeof(Serilog.Events.LogEventLevel), levelSwitch);                
            }
            catch(Exception e)
            {
                throw new ArgumentException($"Accepting one of ({string.Join(",", Enum.GetNames(typeof(Serilog.Events.LogEventLevel)))})", e);
            }
            return ByLogFileName(fileName, new LoggingLevelSwitch(level));
        }

        /// <summary>
        /// New logging instance.
        /// </summary>
        /// <param name="fileName">The file name to write to</param>
        /// <param name="levelSwitch">The minimum level switch</param>
        /// <returns>The bound logger instance</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static Logger ByLogFileName(string fileName, LoggingLevelSwitch levelSwitch)
        {
            var instance = new Logger();
            
            var logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.File(
                    fileName,
                    buffered: false,
                    rollingInterval: RollingInterval.Day, 
                    rollOnFileSizeLimit: true, 
                    outputTemplate: instance.MessageTemplate)
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.FromLogContext()
                .CreateLogger();

            instance.LoggerFactory.AddSerilog(logger, true);
            instance.DefaultLog = logger.ForContext<Logger>();

            instance.DefaultLog.Information("Logger has been triggered by new run request.");

            return instance;
        }

        /// <summary>
        /// New logging instance writing by async background thread to file.
        /// </summary>
        /// <param name="fileName">The file name to write to</param>
        /// <param name="levelSwitch">The minimum level switch</param>
        /// <returns>The bound logger instance</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static Logger ByLogAsyncFileName(string fileName, string levelSwitch = "Debug")
        {
            Serilog.Events.LogEventLevel level;
            try
            {
                level = (Serilog.Events.LogEventLevel)Enum.Parse(typeof(Serilog.Events.LogEventLevel), levelSwitch);                
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Accepting one of ({string.Join(",", Enum.GetNames(typeof(Serilog.Events.LogEventLevel)))})", e);
            }
            return ByLogAsyncFileName(fileName, new LoggingLevelSwitch(level));
        }

        /// <summary>
        /// New logging instance writing by async background thread to file.
        /// </summary>
        /// <param name="fileName">The file name to write to</param>
        /// <param name="levelSwitch">The minimum level switch</param>
        /// <returns>The bound logger instance</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static Logger ByLogAsyncFileName(string fileName, LoggingLevelSwitch levelSwitch)
        {
            var instance = new Logger();

            var logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Async(a => a.File(
                    fileName,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    outputTemplate: instance.MessageTemplate))
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.FromLogContext()
                .CreateLogger();

            instance.LoggerFactory.AddSerilog(logger, true);
            instance.DefaultLog = logger.ForContext<Logger>();

            instance.DefaultLog.Information("Logger has been triggered by new run request.");            

            return instance;
        }
    }
}
