using System;

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
        /// <summary>
        /// Global logging factory.
        /// </summary>
        public static readonly ILoggerFactory LoggingFactory;

        const string messageTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} ({ThreadId} '{ThreadName}'){NewLine}{Exception}";

        // TODO Flush / close when exiting
        static GlobalLogging()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var ownVersion = typeof(GlobalLogging).Assembly.GetName().Version;
            var dynamoVersion = typeof(IsVisibleInDynamoLibraryAttribute).Assembly.GetName().Version;
            var logger = new LoggerConfiguration()
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
            
            LoggingFactory = new LoggerFactory().AddSerilog(logger, true);
            Serilog.Log.Logger = logger;

            logger.Information($"Started TRexIfc-{ownVersion} on Dynamo-{dynamoVersion} host at {DateTime.Now}.");
        }
    }
}
