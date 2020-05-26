using System;
using System.Collections.Generic;

using Bitub.Ifc;
using Bitub.Ifc.Transform.Requests;
using Bitub.Ifc.Transform;

using Store;
using Log;

using Autodesk.DesignScript.Runtime;
using Internal;
using Xbim.Ifc4.UtilityResource;

using Autodesk.DesignScript.Geometry;

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

        private readonly IIfcTransformRequest _ifcTransformRequest;

        internal IfcTransform(IIfcTransformRequest transformRequest)
        {
            _ifcTransformRequest = transformRequest;
        }

        private static ActionType TransformActionToActionType(TransformAction a)
        {
            switch (a)
            {
                case TransformAction.Added:
                    return ActionType.Added | ActionType.Transformed;
                case TransformAction.Modified:
                    return ActionType.Modified | ActionType.Transformed;
                case TransformAction.NotTransferred:
                    return ActionType.Removed | ActionType.Transformed;
                case TransformAction.Transferred:
                    return ActionType.Copied | ActionType.Transformed;

                default:
                    return ActionType.Changed | ActionType.Transformed;
            }
        }

        private static IEnumerable<LogMessage> TransformLogToMessage(string canonicalFrag, IEnumerable<TransformLogEntry> logEntries, ActionType filter = ActionType.Any)
        {
            foreach (var entry in logEntries)
            {
                var action = TransformActionToActionType(entry.PerformedAction);
                if (filter.HasFlag(action)) yield return LogMessage.BySeverityAndMessage(
                    Severity.Info,
                    action, "'{0}': #{1} {2}",
                    canonicalFrag,
                    entry.InstanceHandle?.EntityLabel.ToString() ?? "(not set)",
                    entry.InstanceHandle?.EntityExpressType.Name ?? "(type unknown)");
            }
        }

        #endregion

        [IsVisibleInDynamoLibrary(false)]
        public static IfcModel CreateIfcModelTransform(IfcModel source, IfcTransform transform, string canonicalFrag)
        {
            return IfcStore.CreateFromTransform(source, (model, node) =>
            {
                var task = transform._ifcTransformRequest.Run(model, node);
                // TODO Timeout
                task.Wait();
                
                if (task.IsCompleted)
                {
                    if (node is NodeProgressing np)
                        np.NotifyFinish(ActionType.Changed, false);

                    using (var result = task.Result)
                    {
                        switch (result.ResultCode)
                        {
                            case TransformResult.Code.Finished:
                                foreach (var logMessage in TransformLogToMessage($"{transform._ifcTransformRequest.Name}({canonicalFrag})", result.Log))
                                    node.ActionLog.Add(logMessage);

                                return result.Target;
                            case TransformResult.Code.Canceled:
                                node.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                    Severity.Error, ActionType.Any, "Canceled by user request ({0}).", node.Name));
                                break;
                            case TransformResult.Code.ExitWithError:
                                node.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                    Severity.Error, ActionType.Any, "Caught error ({0}): {1}", node.Name, result.Cause));
                                break;
                        }
                    }
                }
                else
                {
                    if (node is NodeProgressing np)
                        np.NotifyFinish(ActionType.Changed, true);

                    node.ActionLog.Add(LogMessage.BySeverityAndMessage(
                        Severity.Error, ActionType.Changed, $"Task incompletely terminated (Status {task.Status})."));
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
