using System;
using System.Collections.Generic;

using Bitub.Ifc;
using Bitub.Ifc.Transform.Requests;
using Bitub.Ifc.Transform;

using Store;
using Log;

using Autodesk.DesignScript.Runtime;
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
                    return ActionType.Added;
                case TransformAction.Modified:
                    return ActionType.Modified;
                case TransformAction.NotTransferred:
                    return ActionType.Removed;
                case TransformAction.Transferred:
                    return ActionType.Copied;

                default:
                    return ActionType.Changed;
            }
        }

        private static IEnumerable<LogMessage> TransformLogToMessage(string canonicalFrag, IEnumerable<TransformLogEntry> logEntries)
        {
            foreach (var entry in logEntries)
            {
                yield return LogMessage.BySeverityAndMessage(
                    Severity.Info,
                    TransformActionToActionType(entry.PerformedAction), "{0}: #{1} {2}",
                    canonicalFrag,
                    entry.InstanceHandle?.EntityLabel.ToString() ?? "(not set)",
                    entry.InstanceHandle?.EntityExpressType.Name ?? "(type unknown)");
            }
        }

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

        #endregion

#pragma warning restore CS1591


        /// <summary>
        /// Creates new transform request removing property sets from IFC models.
        /// </summary>
        /// <param name="blackListPSets">Blacklist of property set names</param>
        /// <param name="caseSensitiveMatching">Whether to use case sensitive matching</param>
        /// <param name="logInstance">The logging instance</param>
        /// <param name="newMetadata">The new author's meta data</param>
        /// <returns>A ready transform request</returns> 
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
    }
}
