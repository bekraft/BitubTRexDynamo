using System;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Core;

using Autodesk.DesignScript.Runtime;

namespace TRex.Log
{
    /// <summary>
    /// Logger instance capturing logging events to file.
    /// </summary>
    public sealed class Logger
    {
#pragma warning disable CS1591

        #region Internals

        private static readonly string MessageTemplate =         
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} ({ThreadId} '{ThreadName}'){NewLine}{Exception}";

        private Logger(Serilog.ILogger logger)
        {
            LoggerFactory = new LoggerFactory();
            LoggerFactory.AddSerilog(logger, true);
            DefaultLog = logger.ForContext<Logger>();
            DefaultLog.Information("Logging started.");
        }

        /// <summary>
        /// Logger factory
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// The default log.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public Serilog.ILogger DefaultLog { get; private set; }

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

        /// <summary>
        /// New logging instance.
        /// </summary>
        /// <param name="fileName">The file name to write to</param>
        /// <param name="levelSwitch">The minimum level switch</param>
        /// <returns>The bound logger instance</returns>
        private static Logger ByLogFileName(string fileName, LoggingLevelSwitch levelSwitch)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.File(
                    fileName,
                    buffered: false,
                    rollingInterval: RollingInterval.Day, 
                    rollOnFileSizeLimit: true, 
                    outputTemplate: MessageTemplate)
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.FromLogContext()
                .CreateLogger();
            
            return new Logger(logger);
        }

        /// <summary>
        /// New logging instance writing by async background thread to file.
        /// </summary>
        /// <param name="fileName">The file name to write to</param>
        /// <param name="levelSwitch">The minimum level switch</param>
        /// <returns>The bound logger instance</returns>
        private static Logger ByLogAsyncFileName(string fileName, LoggingLevelSwitch levelSwitch)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Async(a => a.File(
                    fileName,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    outputTemplate: MessageTemplate))
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.FromLogContext()
                .CreateLogger();

            return new Logger(logger);
        }
        
        #endregion

        /// <summary>
        /// New logging instance writing by async background thread to file.
        /// </summary>
        /// <param name="fileName">The file name to write to</param>
        /// <param name="levelSwitch">The minimum level switch</param>
        /// <returns>The bound logger instance</returns>
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
        /// New logging instance.
        /// </summary>
        /// <param name="fileName">The file name to write to</param>
        /// <param name="levelSwitch">The minimum level switch</param>
        /// <returns>The bound logger instance</returns>
        public static Logger ByLogFileName(string fileName, string levelSwitch = "Debug")
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
            return ByLogFileName(fileName, new LoggingLevelSwitch(level));
        }
    }
    
#pragma warning restore CS1591

}
