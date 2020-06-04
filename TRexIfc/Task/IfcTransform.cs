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

        private readonly static ILogger Log = GlobalLogging.LoggingFactory.CreateLogger<IfcTransform>();

        private readonly IIfcTransformRequest _ifcTransformRequest;

        internal IfcTransform(IIfcTransformRequest transformRequest)
        {
            _ifcTransformRequest = transformRequest;
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
                    return LogReason.Changed | LogReason.Transformed;
            }
        }

        private static IEnumerable<LogMessage> TransformLogToMessage(string canonicalFrag, IEnumerable<TransformLogEntry> logEntries, LogReason filter = LogReason.Any)
        {
            foreach (var entry in logEntries)
            {
                var action = TransformActionToActionType(entry.PerformedAction);
                if (filter.HasFlag(action)) yield return LogMessage.BySeverityAndMessage(
                    LogSeverity.Info,
                    action, "'{0}': #{1} {2}",
                    canonicalFrag,
                    entry.InstanceHandle?.EntityLabel.ToString() ?? "(not set)",
                    entry.InstanceHandle?.EntityExpressType.Name ?? "(type unknown)");
            }
        }

        private static bool TryCastEnum<T>(object objFilterMask, out T member) where T : Enum
        {
            member = default(T);
            bool isCasted = true;
            if (objFilterMask is T a)
                member = a;
            else if (objFilterMask is string s)
                member = (T)Enum.Parse(typeof(T), s);
            else if (objFilterMask is int i)
                member = (T)Enum.ToObject(typeof(T), i);
            else
            {
                Log.LogWarning($"Couldn't cast '{objFilterMask}' to {nameof(T)}");
                isCasted = false;
            }

            return isCasted;
        }

        #endregion

        [IsVisibleInDynamoLibrary(false)]
        public static IfcModel CreateIfcModelTransform(IfcModel source, IfcTransform transform, string canonicalFrag, object objFilterMask)
        {
            LogReason filterMask;
            if (!TryCastEnum(objFilterMask, out filterMask))
                Log.LogInformation($"Using default filter: {LogReason.Any}");

            return IfcStore.CreateFromTransform(source, (model, node) =>
            {
                Log.LogInformation($"Starting '{transform._ifcTransformRequest.Name}' on {node.Name} ...");
                var task = transform._ifcTransformRequest.Run(model, node);
                // TODO Timeout
                task.Wait();
                Log.LogInformation($"Finalized '{transform._ifcTransformRequest.Name}' on {node.Name}.");

                if (task.IsCompleted)
                {
                    if (node is NodeProgressing np)
                        np.NotifyFinish(LogReason.Changed, false);

                    using (var result = task.Result)
                    {
                        switch (result.ResultCode)
                        {
                            case TransformResult.Code.Finished:
                                var name = $"{transform._ifcTransformRequest.Name}({canonicalFrag})";
                                foreach (var logMessage in TransformLogToMessage(name, result.Log, filterMask | LogReason.Transformed))
                                {
                                    node.ActionLog.Add(logMessage);
                                }
                                return result.Target;
                            case TransformResult.Code.Canceled:
                                node.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                    LogSeverity.Error, LogReason.Any, "Canceled by user request ({0}).", node.Name));
                                break;
                            case TransformResult.Code.ExitWithError:
                                node.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                    LogSeverity.Error, LogReason.Any, "Caught error ({0}): {1}", node.Name, result.Cause));
                                break;
                        }
                    }
                }
                else
                {
                    if (node is NodeProgressing np)
                        np.NotifyFinish(LogReason.Changed, true);

                    node.ActionLog.Add(LogMessage.BySeverityAndMessage(
                        LogSeverity.Error, LogReason.Changed, $"Task incompletely terminated (Status {task.Status})."));
                }
                return null;
            }, canonicalFrag);
        }

        [IsVisibleInDynamoLibrary(false)]
        public override string ToString()
        {
            return _ifcTransformRequest?.Name ?? "Anonymous IfcTransform";
        }

        [IsVisibleInDynamoLibrary(false)]
        public static IfcTransform RemovePropertySetsRequest(Logger logInstance, IfcAuthorMetadata newMetadata, string[] blackListPSets, bool caseSensitiveMatching)
        {
            return new IfcTransform(new IfcPropertySetRemovalRequest(logInstance.LoggerFactory)
            {
                BlackListNames = blackListPSets,
                IsNameMatchingCaseSensitive = caseSensitiveMatching,
                IsLogEnabled = true,
                EditorCredentials = newMetadata.MetaData.ToEditorCredentials()
            });
        }

        [IsVisibleInDynamoLibrary(false)]
        public static IfcTransform TransformAxisAlignmentRequest(Logger logInstance, IfcAuthorMetadata newMetadata, Alignment alignment, IfcPlacementStrategy placementStrategy)
        {
            return new IfcTransform(new IfcPlacementTransformRequest(logInstance.LoggerFactory)
            {
                AxisAlignment = alignment.TheAxisAlignment,
                IsLogEnabled = true,
                PlacementStrategy = placementStrategy,
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
