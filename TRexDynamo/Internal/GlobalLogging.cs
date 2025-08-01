using System;
using System.Diagnostics;

using Autodesk.DesignScript.Runtime;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Core;

namespace TRex.Internal;

/// <summary>
/// Global logging configuration.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed class GlobalLogging : IDisposable
{
    #region Internals

    internal static readonly Stopwatch DiagnosticStopWatch;
    
    private const string MessageTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} ({ThreadId} '{ThreadName}'){NewLine}{Exception}";

    private GlobalLogging(String userProfile, Version? trexVersion, Version? dynamoVersion)
    {
        Version = trexVersion;
        DynamoVersion = dynamoVersion;
        UserProfile = userProfile;
        
        Log = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Debug))
            .WriteTo.Async(a => a.File(
                $"{userProfile}\\TRexIfc-{trexVersion?.Major}.{trexVersion?.Minor}_Dynamo-{dynamoVersion?.Major}.{dynamoVersion?.Minor}_.log",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                outputTemplate: MessageTemplate), bufferSize: 500)
            /*.WriteTo.File(
                $"{userProfile}\\TRexIfc-{ownVersion.Major}.{ownVersion.Minor}_Dynamo-{dynamoVersion.Major}.{dynamoVersion.Minor}_.log",
                buffered: false,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                outputTemplate: MessageTemplate) */
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .Enrich.FromLogContext()
            .CreateLogger();
        
        LoggingFactory.AddSerilog(Log, true);
    }

    ~GlobalLogging()
    {
        Dispose();
    }

    #endregion

    /// <summary>
    /// Global logging factory.
    /// </summary>
    public static readonly ILoggerFactory LoggingFactory = new LoggerFactory();
    
    /// <summary>
    /// The instance of global logging.
    /// </summary>
    public static readonly GlobalLogging Instance;
    
    /// <summary>
    /// User profile
    /// </summary>
    public readonly string UserProfile;

    /// <summary>
    /// Singleton logging instance.
    /// </summary>
    public readonly Logger Log;

    /// <summary>
    /// Dynamo TRex Version.
    /// </summary>
    public readonly Version? Version;
    
    /// <summary>
    /// Dynamo Version.
    /// </summary>
    public readonly Version? DynamoVersion;
    
    static GlobalLogging()
    {
        DiagnosticStopWatch = new Stopwatch();

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var trexVersion = typeof(GlobalLogging).Assembly.GetName().Version;
        var dynamoVersion = typeof(IsVisibleInDynamoLibraryAttribute).Assembly.GetName().Version;
        
        Instance = new GlobalLogging(userProfile, trexVersion, dynamoVersion);
        Serilog.Log.Logger = Instance.Log;

        Instance.Log.Information($"Started DynamoTRex {trexVersion} on Dynamo {dynamoVersion} at {DateTime.Now}.");
    }
    
    /// <summary>
    /// Dispose global logging instance.
    /// </summary>
    public void Dispose()
    {
        Serilog.Log.Information("Stopping DynamoTRex {Version} on Dynamo {DynamoVersion} at {DateTime.Now}.", 
            Version, DynamoVersion, DateTime.Now);
        Serilog.Log.CloseAndFlush();
    }
}
