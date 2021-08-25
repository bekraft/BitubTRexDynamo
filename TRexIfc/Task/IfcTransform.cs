using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Bitub.Ifc.Transform;

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

        private readonly static ILogger log = GlobalLogging.loggingFactory.CreateLogger<IfcTransform>();

        private readonly IModelTransform transformDelegate;

        internal int TimeOutMillis { get; set; } = -1;

        internal CancellationTokenSource CancellationSource { get; private set; }

        internal string Mark { get; set; } = $"{DateTime.Now.Ticks}";

        internal IfcTransform(IModelTransform transform)
        {
            transformDelegate = transform;
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

        private static IEnumerable<LogMessage> TransformLogToMessage(string storeName, IEnumerable<TransformLogEntry> logEntries, LogReason filter = LogReason.Any)
        {
            foreach (var entry in logEntries)
            {
                var action = TransformActionToActionType(entry.performed);
                if (LogReason.None != (filter & action))
                {
                    yield return LogMessage.BySeverityAndMessage(
                        storeName,
                        LogSeverity.Info,
                        action, "#{0} {1}",
                        entry.handle.EntityLabel.ToString() ?? "(not set)",
                        entry.handle.EntityExpressType.Name ?? "(type unknown)");
                }
            }
        }

        #endregion

        [IsVisibleInDynamoLibrary(false)]
        public static IfcModel BySourceAndTransform(IfcModel source, IfcTransform transform, string nameAddon, object objFilterMask)
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
                log.LogInformation("Starting '{1}' ({0}) on {2} ...", node.GetHashCode(), transform.transformDelegate.Name, node.Name);
                try
                {
                    using (var task = transform.transformDelegate.Run(model, node.CreateProgressMonitor(LogReason.Transformed)))
                    {
                        task.Wait(transform.TimeOutMillis, transform.CancellationSource.Token);

                        log.LogInformation("Finalized '{1}' ({0}) on {2}.", node.GetHashCode(), transform.transformDelegate.Name, node.Name);

                        if (task.IsCompleted)
                        {
                            if (node is ProgressingTask np)
                                np.OnProgressEnded(LogReason.Changed, false);

                            using (var result = task.Result)
                            {
                                switch (result.ResultCode)
                                {
                                    case TransformResult.Code.Finished:
                                        var name = $"{transform.transformDelegate.Name}({node.Name})";
                                        foreach (var logMessage in TransformLogToMessage(name, result.Log, filterMask))
                                        {
                                            node.ActionLog.Add(logMessage);
                                        }
                                        return result.Target;
                                    case TransformResult.Code.Canceled:
                                        node.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                            node.Name, LogSeverity.Error, LogReason.Any, "Canceled by user request ({0}).", node.Name));
                                        break;
                                    case TransformResult.Code.ExitWithError:
                                        node.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                            node.Name, LogSeverity.Error, LogReason.Any, "Caught error ({0}): {1}", node.Name, result.Cause));
                                        break;
                                }
                            }
                        }
                        else
                        {
                            if (node is ProgressingTask np)
                                np.OnProgressEnded(LogReason.Changed, true);

                            node.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                node.Name, LogSeverity.Error, LogReason.Changed, $"Task incompletely terminated (Status {task.Status})."));
                        }
                        return null;
                    }
                } 
                catch(Exception thrownOnExec)
                {
                    log.LogError("{0} '{1}'\n{2}", thrownOnExec, thrownOnExec.Message, thrownOnExec.StackTrace);
                    throw new Exception("Exception while executing task");
                }
            }, nameAddon);
        }

        [IsVisibleInDynamoLibrary(false)]
        public override string ToString()
        {
            return transformDelegate?.Name ?? "Anonymous IfcTransform";
        }

        [IsVisibleInDynamoLibrary(false)]
        public static IfcTransform NewRemovePropertySetsRequest(Logger logInstance, IfcAuthorMetadata newMetadata, 
            string[] removePropertySets, string[] keepPropertySets, bool? caseSensitiveMatching)
        {
            return new IfcTransform(new ModelPropertySetRemovalTransform(logInstance?.LoggerFactory, defaultLogFilter)
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
            ModelPlacementStrategy strategy = default(ModelPlacementStrategy);
            if (!DynamicArgumentDelegation.TryCastEnum(placementStrategy, out strategy))
                log.LogWarning("Unable to cast '{0}' to type {1}. Using '{2}'.", placementStrategy, nameof(ModelPlacementStrategy), strategy);

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
            ProductRefactorStrategy strategy = default(ProductRefactorStrategy);
            if (!DynamicArgumentDelegation.TryCastEnum(refactorStrategy, out strategy))
                log.LogWarning("Unable to cast '{0}' to type {1}. Using '{2}'.", refactorStrategy, nameof(ProductRefactorStrategy), strategy);

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
