using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Autodesk.DesignScript.Runtime;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Core;

namespace Internal
{
    /// <summary>
    /// Global logging configuration.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public sealed class GlobalLogging
    {
        internal static readonly Stopwatch DiagnosticStopWatch;
        
        /// <summary>
        /// Global logging factory.
        /// </summary>
        public static readonly ILoggerFactory loggingFactory;

        /// <summary>
        /// Singleton logging instance.
        /// </summary>
        public static readonly Serilog.ILogger log;

        private const string messageTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} ({ThreadId} '{ThreadName}'){NewLine}{Exception}";

        // TODO Flush / close when exiting
        static GlobalLogging()
        {
            DiagnosticStopWatch = new Stopwatch();

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var ownVersion = typeof(GlobalLogging).Assembly.GetName().Version;
            var dynamoVersion = typeof(IsVisibleInDynamoLibraryAttribute).Assembly.GetName().Version;
            log = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Debug))
                /*.WriteTo.Async(a => a.File(
                    $"{userProfile}\\TRexIfc-{ownVersion.Major}.{ownVersion.Minor}_Dynamo-{dynamoVersion.Major}.{dynamoVersion.Minor}_.log",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    outputTemplate: messageTemplate), bufferSize: 500)*/
                .WriteTo.File(
                    $"{userProfile}\\TRexIfc-{ownVersion.Major}.{ownVersion.Minor}_Dynamo-{dynamoVersion.Major}.{dynamoVersion.Minor}_.log",
                    buffered: false,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    outputTemplate: messageTemplate) 
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.FromLogContext()
                .CreateLogger();
            
            loggingFactory = new LoggerFactory().AddSerilog(log, true);
            Serilog.Log.Logger = log;

            log.Information($"Started TRexIfc-{ownVersion} on Dynamo-{dynamoVersion} host at {DateTime.Now}.");
        }
    }
}
