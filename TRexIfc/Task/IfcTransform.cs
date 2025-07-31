using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Bitub.Xbim.Ifc.Transform;

using Autodesk.DesignScript.Runtime;

using TRex.Geom;
using TRex.Store;
using TRex.Log;
using TRex.Internal;
using System.Threading;

namespace TRex.Task
{
    /// <summary>
    /// Transforming model delegates.
    /// </summary>    
    public class IfcTransform
    {
        // Disable comment warning
#pragma warning disable CS1591

        #region Internals

        private static readonly TransformActionResult[] defaultLogFilter = new[]
        {
            TransformActionResult.Skipped, 
            TransformActionResult.Modified, 
            TransformActionResult.Added
        };

        private static readonly ILogger Log = GlobalLogging.LoggingFactory.CreateLogger<IfcTransform>();

        private readonly IModelTransform TransformDelegate;

        private int TimeOutMillis { get; set; } = -1;

        private CancellationTokenSource? CancellationSource { get; set; }

        private string Mark { get; set; } = $"{DateTime.Now.Ticks}";

        private IfcTransform(IModelTransform transform)
        {
            TransformDelegate = transform;
        }

        private static LogReason TransformActionToActionType(TransformActionResult a)
        {
            switch (a)
            {
                case TransformActionResult.Added:
                    return LogReason.Added | LogReason.Transformed;
                case TransformActionResult.Modified:
                    return LogReason.Modified | LogReason.Transformed;
                case TransformActionResult.Skipped:
                    return LogReason.Removed | LogReason.Transformed;
                case TransformActionResult.Copied:
                    return LogReason.Copied | LogReason.Transformed;

                default:
                    return LogReason.Changed;
            }
        }

        private static IEnumerable<LogMessage> TransformLogToMessage(string storeName, 
            IEnumerable<TransformLogEntry> logEntries, LogReason filter = LogReason.Any)
        {
            foreach (var entry in logEntries)
            {
                var action = TransformActionToActionType(entry.Performed);
                if (LogReason.None != (filter & action))
                {
                    yield return LogMessage.BySeverityAndMessage(
                        storeName,
                        LogSeverity.Info,
                        action, "#{0} {1}",
                        entry.Handle.EntityLabel.ToString() ?? "(not set)",
                        entry.Handle.EntityExpressType.Name ?? "(type unknown)");
                }
            }
        }

        #endregion

        [IsVisibleInDynamoLibrary(false)]
        public static IfcModel? BySourceAndTransform(IfcModel? source, IfcTransform? transform, string? nameAddon, object objFilterMask)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == transform)
                throw new ArgumentNullException(nameof(transform));

            if (null == nameAddon)
                nameAddon = transform.Mark;

            LogReason filterMask = DynamicArgumentDelegation.TryCastEnumOrDefault(objFilterMask, LogReason.Any);

            if (null == transform.CancellationSource)
                transform.CancellationSource = new CancellationTokenSource();

            return IfcStore.ByTransform(source, (model, node) =>
            {
                Log.LogInformation("Starting '{Code}' ({Name}) on {NodeName} ...", node.GetHashCode(), transform.TransformDelegate.Name, node.Name);
                try
                {
                    using (var task = transform.TransformDelegate.Run(model, node.CreateProgressMonitor(LogReason.Transformed)))
                    {
                        task.Wait(transform.TimeOutMillis, transform.CancellationSource.Token);

                        Log.LogInformation("Finalized '{Code}' ({Name}) on {NodeName}.", node.GetHashCode(), transform.TransformDelegate.Name, node.Name);

                        if (task.IsCompleted)
                        {
                            if (node is ProgressingTask np)
                                np.OnProgressEnded(LogReason.Changed, false);

                            using (var result = task.Result)
                            {
                                switch (result.ResultCode)
                                {
                                    case TransformResult.Code.Finished:
                                        var name = $"{transform.TransformDelegate.Name}({node.Name})";
                                        node.OnActionLogged(TransformLogToMessage(name, result.Log, filterMask).ToArray());                                   
                                        return result.Target;
                                    case TransformResult.Code.Canceled:
                                        node.OnActionLogged(LogMessage.BySeverityAndMessage(
                                            node.Name, LogSeverity.Error, LogReason.Any, "Canceled by user request ({0}).", node.Name));
                                        break;
                                    case TransformResult.Code.ExitWithError:
                                        node.OnActionLogged(LogMessage.BySeverityAndMessage(
                                            node.Name, LogSeverity.Error, LogReason.Any, "Caught error ({0}): {1}", node.Name, result.Cause));
                                        break;
                                }
                            }
                        }
                        else
                        {
                            if (node is ProgressingTask np)
                                np.OnProgressEnded(LogReason.Changed, true);

                            node.OnActionLogged(LogMessage.BySeverityAndMessage(
                                node.Name, LogSeverity.Error, LogReason.Changed, $"Task incompletely terminated (Status {task.Status})."));
                        }
                        return null;
                    }
                } 
                catch(Exception thrownOnExec)
                {
                    Log.LogError("{Exception} '{Message}'\n{StackTrace}", thrownOnExec, thrownOnExec.Message, thrownOnExec.StackTrace);
                    throw new Exception("Exception while executing task");
                }
            }, nameAddon);
        }

        [IsVisibleInDynamoLibrary(false)]
        public override string ToString()
        {
            return TransformDelegate?.Name ?? "Anonymous IfcTransform";
        }

        [IsVisibleInDynamoLibrary(false)]
        public static IfcTransform NewRemovePropertySetsRequest(Logger logInstance, IfcAuthorMetadata newMetadata, 
            string[] removePropertySets, string[] keepPropertySets, bool? caseSensitiveMatching)
        {
            return new IfcTransform(new PropertySetRemovalTransform(logInstance?.LoggerFactory, defaultLogFilter)
            {
                ExludePropertySetByName = removePropertySets,
                IncludePropertySetByName = keepPropertySets,
                IsNameMatchingCaseSensitive = caseSensitiveMatching ?? false,
                FilterRuleStrategy = FilterRuleStrategyType.IncludeBeforeExclude,
                EditorCredentials = newMetadata?.MetaData.ToEditorCredentials(),
            });
        }

        [IsVisibleInDynamoLibrary(false)]
        public static IfcTransform NewTransformPlacementRequest(Logger logInstance, IfcAuthorMetadata newMetadata, Alignment alignment, object placementStrategy)
        {
            if (!DynamicArgumentDelegation.TryCastEnum(placementStrategy, out ModelPlacementStrategy strategy))
                Log.LogWarning("Unable to cast '{0}' to type {1}. Using '{2}'.", placementStrategy, nameof(ModelPlacementStrategy), strategy);

            return new IfcTransform(new ModelPlacementTransform(logInstance?.LoggerFactory, defaultLogFilter)
            {
                AxisAlignment = alignment.TheAxisAlignment,
                PlacementStrategy = strategy,
                EditorCredentials = newMetadata?.MetaData.ToEditorCredentials()
            });
        }

        [IsVisibleInDynamoLibrary(false)]
        public static IfcTransform NewRepresentationRefactorTransform(Logger logInstance, IfcAuthorMetadata newMetadata, string[] contexts, object refactorStrategy)
        {
            if (!DynamicArgumentDelegation.TryCastEnum(refactorStrategy, out ProductRefactorStrategy strategy))
                Log.LogWarning("Unable to cast '{0}' to type {1}. Using '{2}'.", refactorStrategy, nameof(ProductRefactorStrategy), strategy);

            return new IfcTransform(new ProductRepresentationRefactorTransform(logInstance?.LoggerFactory, defaultLogFilter)
            {
                ContextIdentifiers = contexts,
                Strategy = strategy,                
                EditorCredentials = newMetadata?.MetaData.ToEditorCredentials()
            });
        }

#pragma warning restore CS1591
    }
}
