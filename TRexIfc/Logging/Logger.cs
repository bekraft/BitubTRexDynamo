using System;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Core;

using Autodesk.DesignScript.Runtime;

namespace TRexIfc.Logging
{
    /// <summary>
    /// Logger instance capturing logging events to file.
    /// </summary>
    public class Logger
    {
        #region Internals

        /// <summary>
        /// Logger factory
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public ILoggerFactory LoggerFactory { get; private set; }

        internal string MessageTemplate { get; set; } =         
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} ({ThreadId}){NewLine}{Exception}";

        internal Logger()
        {
            LoggerFactory = new LoggerFactory();            
        }

        #endregion

        /// <summary>
        /// New logging instance.
        /// </summary>
        /// <param name="fileName">The file name to write to</param>
        /// <param name="levelSwitch">The minimum level switch</param>
        /// <returns>The bound logger instance</returns>
        public static Logger ByLogFileName(string fileName, string levelSwitch = "Debug")
        {
            try
            {
                var level = (Serilog.Events.LogEventLevel)Enum.Parse(typeof(Serilog.Events.LogEventLevel), levelSwitch);
                return ByLogFileName(fileName, new LoggingLevelSwitch(level));
            }
            catch(Exception e)
            {
                throw new ArgumentException($"Accepting one of ({string.Join(",", Enum.GetNames(typeof(Serilog.Events.LogEventLevel)))}");
            }            
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
            instance.LoggerFactory.AddSerilog();
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.File(
                    fileName, 
                    rollingInterval: RollingInterval.Day, 
                    rollOnFileSizeLimit: true, 
                    outputTemplate: instance.MessageTemplate)
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .CreateLogger();

            return instance;
        }

        /// <summary>
        /// New logging instance writing by async background thread to file.
        /// </summary>
        /// <param name="fileName">The file name to write to</param>
        /// <param name="levelSwitch">The minimum level switch</param>
        /// <returns>The bound logger instance</returns>
        public static Logger ByLogAsyncFileName(string fileName, string levelSwitch = "Debug")
        {
            try
            {
                var level = (Serilog.Events.LogEventLevel)Enum.Parse(typeof(Serilog.Events.LogEventLevel), levelSwitch);
                return ByLogAsyncFileName(fileName, new LoggingLevelSwitch(level));
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Accepting one of ({string.Join(",", Enum.GetNames(typeof(Serilog.Events.LogEventLevel)))}");
            }
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
            instance.LoggerFactory.AddSerilog();
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Async(a => a.File(
                    fileName, 
                    rollingInterval: RollingInterval.Day, 
                    rollOnFileSizeLimit: true, 
                    outputTemplate: instance.MessageTemplate))
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .CreateLogger();

            return instance;
        }

    }
}
