using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

using Bitub.Ifc.Transform;

using Store;
using Log;

using Autodesk.DesignScript.Runtime;

namespace Task
{
    /// <summary>
    /// Transforming model delegates.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public class IfcTransform
    {
        #region Internals

        internal TimeSpan TimeOut { get; set; } = TimeSpan.MaxValue;

        internal IfcTransform()
        {
        }

        #endregion

        /// <summary>
        /// New IFC transform handler with given time out
        /// </summary>
        /// <param name="minutes">The maximum minutes</param>
        /// <param name="seconds">The maximum seconds</param>
        /// <returns></returns>
        public static IfcTransform ByTimeOut(int minutes, int seconds)
        {
            return new IfcTransform { TimeOut = TimeSpan.FromSeconds(minutes * 60 + seconds) };
        }

        /// <summary>
        /// Removes IFC property sets by their names.
        /// </summary>
        /// <param name="prefs">The request preferences</param>
        /// <param name="ifcModel">The IFC model producer</param>        
        /// <returns>New IFC model</returns>  
        [IsVisibleInDynamoLibrary(false)]
        public IfcModel RemovePropertySets(PSetRemovalRequest prefs, IfcModel ifcModel)
        {
            return IfcStore.CreateFromTransform(ifcModel, (model, node) =>
            {
                var task = prefs.Request.Run(model, node);
                task.Wait(TimeOut);

                if (task.IsCompleted)
                {
                    using (var result = task.Result)
                    {
                        switch (result.ResultCode)
                        {
                            case TransformResult.Code.Finished:
                                return result.Target;
                            case TransformResult.Code.Canceled:
                                node.LogMessages.Add(LogMessage.BySeverityAndMessage(
                                    Severity.Error, ActionType.Any, "Canceled by user request ({0}).", ifcModel.Name));
                                break;
                            case TransformResult.Code.ExitWithError:
                                node.LogMessages.Add(LogMessage.BySeverityAndMessage(
                                    Severity.Error, ActionType.Any, "Caught error ({0}): {1}", ifcModel.Name, result.Cause));                                
                                break;
                        }
                    }
                }
                else
                {                   
                    node.LogMessages.Add(LogMessage.BySeverityAndMessage(
                        Severity.Error, ActionType.Change, $"Task incompletely terminated (Status {task.Status})."));
                }
                return null;
            }, prefs.NameSuffix);
        }

    }
}
