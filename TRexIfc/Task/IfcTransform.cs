using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Xbim.Ifc4.UtilityResource;

using Bitub.Ifc;
using Bitub.Ifc.Transform.Requests;
using Bitub.Ifc.Transform;

using Autodesk.DesignScript.Runtime;

using Geom;
using Store;
using Log;
using Internal;
using System.Threading;

namespace Task
{
    /// <summary>
    /// Transforming model delegates.
    /// </summary>    
    public class IfcTransform
    {
        // Disable comment warning
#pragma warning disable CS1591

        #region Internals

        private readonly static ILogger Log = GlobalLogging.loggingFactory.CreateLogger<IfcTransform>();

        private readonly IIfcTransformRequest Request;

        internal int TimeOutMillis { get; set; } = -1;

        internal CancellationTokenSource CancellationSource { get; private set; }

        internal string Mark { get; set; } = $"{DateTime.Now.ToBinary()}";

        internal IfcTransform(IIfcTransformRequest transformRequest)
        {
            Request = transformRequest;
        }

        private static LogReason TransformActionToActionType(TransformAction a)
        {
            switch (a)
            {
                case TransformAction.Added:
                    return LogReason.Added | LogReason.Transformed;
                case TransformAction.Modified:
                    return LogReason.Modified | LogReason.Transformed;
                case TransformAction.NotTransferred:
                    return LogReason.Removed | LogReason.Transformed;
                case TransformAction.Transferred:
                    return LogReason.Copied | LogReason.Transformed;

                default:
                    return LogReason.Changed;
            }
        }

        private static IEnumerable<LogMessage> TransformLogToMessage(string storeName, IEnumerable<TransformLogEntry> logEntries, LogReason filter = LogReason.Any)
        {
            foreach (var entry in logEntries)
            {
                var action = TransformActionToActionType(entry.PerformedAction);
                if (filter.HasFlag(action))
                {
                    yield return LogMessage.BySeverityAndMessage(
                        storeName,
                        LogSeverity.Info,
                        action, "#{0} {1}",
                        entry.InstanceHandle?.EntityLabel.ToString() ?? "(not set)",
                        entry.InstanceHandle?.EntityExpressType.Name ?? "(type unknown)");
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
                Log.LogInformation("Unable to cast '{0}' of type {1}. Using '{2}'.", objFilterMask, nameof(LogReason), filterMask);

            if (null == transform.CancellationSource)
                transform.CancellationSource = new CancellationTokenSource();

            return IfcStore.ByTransform(source, (model, node) =>
            {
                Log.LogInformation("Starting '{1}' ({0}) on {2} ...", node.GetHashCode(), transform.Request.Name, node.Name);
                try
                {
                    using (var task = transform.Request.Run(model, node.CreateProgressMonitor(LogReason.Transformed)))
                    {
                        task.Wait(transform.TimeOutMillis, transform.CancellationSource.Token);

                        Log.LogInformation("Finalized '{1}' ({0}) on {2}.", node.GetHashCode(), transform.Request.Name, node.Name);

                        if (task.IsCompleted)
                        {
                            if (node is ProgressingTask np)
                                np.OnProgressEnded(LogReason.Changed, false);

                            using (var result = task.Result)
                            {
                                switch (result.ResultCode)
                                {
                                    case TransformResult.Code.Finished:
                                        var name = $"{transform.Request.Name}({node.Name})";
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
                    Log.LogError("{0} '{1}'\n{2}", thrownOnExec, thrownOnExec.Message, thrownOnExec.StackTrace);
                    throw new Exception("Exception while executing task");
                }
            }, canonicalFrag);
        }

        [IsVisibleInDynamoLibrary(false)]
        public override string ToString()
        {
            return Request?.Name ?? "Anonymous IfcTransform";
        }

        [IsVisibleInDynamoLibrary(false)]
        public static IfcTransform NewRemovePropertySetsRequest(Logger logInstance, IfcAuthorMetadata newMetadata, 
            string[] removePropertySets, string[] keepPropertySets, bool caseSensitiveMatching)
        {
            return new IfcTransform(new IfcPropertySetRemovalRequest(logInstance.LoggerFactory)
            {
                ExludePropertySetByName = removePropertySets,
                IncludePropertySetByName = keepPropertySets,
                IsNameMatchingCaseSensitive = caseSensitiveMatching,
                FilterRuleStrategy = FilterRuleStrategyType.IncludeBeforeExclude,
                IsLogEnabled = true,
                EditorCredentials = newMetadata.MetaData.ToEditorCredentials()
            });
        }

        [IsVisibleInDynamoLibrary(false)]
        public static IfcTransform NewTransformPlacementRequest(Logger logInstance, IfcAuthorMetadata newMetadata, Alignment alignment, object placementStrategy)
        {
            IfcPlacementStrategy strategy = default(IfcPlacementStrategy);
            if (!DynamicArgumentDelegation.TryCastEnum(placementStrategy, out strategy))
                Log.LogWarning("Unable to cast '{0}' to type {1}. Using '{2}'.", placementStrategy, nameof(IfcPlacementStrategy), strategy);

            return new IfcTransform(new IfcPlacementTransformRequest(logInstance.LoggerFactory)
            {
                AxisAlignment = alignment.TheAxisAlignment,
                IsLogEnabled = true,
                PlacementStrategy = strategy,
                EditorCredentials = newMetadata.MetaData.ToEditorCredentials()
            });
        }


#pragma warning restore CS1591

        /// <summary>
        /// Rewrites the given GUID as IfcGloballyUniqueId (or also known as IfcGuid base64 representation)
        /// </summary>
        /// <param name="guid">A GUID (i.e. "B47EF7FE-BDF4-4504-8A67-DB697D04F659")</param>
        /// <returns>The IFC Base64 representation</returns>
        public static string GuidToIfcGuid(string guid)
        {            
            return IfcGloballyUniqueId.ConvertToBase64(Guid.Parse(guid));
        }

    }
}
