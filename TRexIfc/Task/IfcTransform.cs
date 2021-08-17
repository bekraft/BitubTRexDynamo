using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Xbim.Ifc4.UtilityResource;

using Bitub.Ifc;
using Bitub.Ifc.Transform.Requests;
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
            TransformActionResult.NotTransferred, 
            TransformActionResult.Modified, 
            TransformActionResult.Added
        };

        private readonly static ILogger log = GlobalLogging.loggingFactory.CreateLogger<IfcTransform>();

        private readonly IModelTransform transformDelegate;

        internal int TimeOutMillis { get; set; } = -1;

        internal CancellationTokenSource CancellationSource { get; private set; }

        internal string Mark { get; set; } = $"{DateTime.Now.ToBinary()}";

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
                case TransformActionResult.NotTransferred:
                    return LogReason.Removed | LogReason.Transformed;
                case TransformActionResult.Transferred:
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
                if (filter.HasFlag(action))
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
        public static IfcModel BySourceAndTransform(IfcModel source, IfcTransform transform, string canonicalFrag, object objFilterMask)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == transform)
                throw new ArgumentNullException(nameof(transform));

            if (null == canonicalFrag)
                canonicalFrag = transform.Mark;

            LogReason filterMask;
            if (!DynamicArgumentDelegation.TryCastEnum(objFilterMask, out filterMask))
                log.LogInformation("Unable to cast '{0}' of type {1}. Using '{2}'.", objFilterMask, nameof(LogReason), filterMask);

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
                                        foreach (var logMessage in TransformLogToMessage(name, result.Log, filterMask | LogReason.Transformed))
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
            }, canonicalFrag);
        }

        [IsVisibleInDynamoLibrary(false)]
        public override string ToString()
        {
            return transformDelegate?.Name ?? "Anonymous IfcTransform";
        }

        [IsVisibleInDynamoLibrary(false)]
        public static IfcTransform NewRemovePropertySetsRequest(Logger logInstance, IfcAuthorMetadata newMetadata, 
            string[] removePropertySets, string[] keepPropertySets, bool caseSensitiveMatching)
        {
            return new IfcTransform(new ModelPropertySetRemovalTransform(logInstance.LoggerFactory, defaultLogFilter)
            {
                ExludePropertySetByName = removePropertySets,
                IncludePropertySetByName = keepPropertySets,
                IsNameMatchingCaseSensitive = caseSensitiveMatching,
                FilterRuleStrategy = FilterRuleStrategyType.IncludeBeforeExclude,
                EditorCredentials = newMetadata.MetaData.ToEditorCredentials(),
            });
        }

        [IsVisibleInDynamoLibrary(false)]
        public static IfcTransform NewTransformPlacementRequest(Logger logInstance, IfcAuthorMetadata newMetadata, Alignment alignment, object placementStrategy)
        {
            ModelPlacementStrategy strategy = default(ModelPlacementStrategy);
            if (!DynamicArgumentDelegation.TryCastEnum(placementStrategy, out strategy))
                log.LogWarning("Unable to cast '{0}' to type {1}. Using '{2}'.", placementStrategy, nameof(ModelPlacementStrategy), strategy);

            return new IfcTransform(new ModelPlacementTransform(logInstance.LoggerFactory, defaultLogFilter)
            {
                AxisAlignment = alignment.TheAxisAlignment,
                PlacementStrategy = strategy,
                EditorCredentials = newMetadata.MetaData.ToEditorCredentials()
            });
        }

        [IsVisibleInDynamoLibrary(false)]
        public static IfcTransform NewRepresentationRefactorTransform(Logger logInstance, IfcAuthorMetadata newMetadata, string[] contexts, object refactorStrategy)
        {
            ProductRepresentationRefactorStrategy strategy = default(ProductRepresentationRefactorStrategy);
            if (!DynamicArgumentDelegation.TryCastEnum(refactorStrategy, out strategy))
                log.LogWarning("Unable to cast '{0}' to type {1}. Using '{2}'.", refactorStrategy, nameof(ProductRepresentationRefactorStrategy), strategy);

            return new IfcTransform(new ProductRepresentationRefactorTransform(logInstance.LoggerFactory, defaultLogFilter)
            {
                ContextIdentifiers = contexts,
                Strategy = strategy,                
                EditorCredentials = newMetadata.MetaData.ToEditorCredentials()
            });
        }

#pragma warning restore CS1591
    }
}
