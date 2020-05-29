using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        static GlobalLogging()
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Debug))
                .WriteTo.Async(a => a.File(
                    "TRexIfcDynamoRun.log",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    outputTemplate: messageTemplate))
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.FromLogContext()
                .CreateLogger();

            LoggingFactory = new LoggerFactory().AddSerilog(logger, true);
        }
    }
}
