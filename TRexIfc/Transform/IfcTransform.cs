using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

using Bitub.Ifc.Transform;

using TRexIfc.Logging;

using Autodesk.DesignScript.Runtime;

namespace TRexIfc.Transform
{
    /// <summary>
    /// Transforming model delegates.
    /// </summary>
    public class IfcTransform
    {
        #region Internals

        internal IfcTransform()
        {
        }

        #endregion

        /// <summary>
        /// Removes IFC property sets by their names.
        /// </summary>
        /// <param name="pref">The request preferences</param>
        /// <param name="storeProducer">The IFC store producer</param>
        /// <param name="taskNode">An optional task node for progress annoucement</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IIfcStoreProducer RemovePropertySets(PSetRemovalRequest pref, IIfcStoreProducer storeProducer, ICancelableTaskNode taskNode)
        {
            return new IfcStoreProducerDelegate(storeProducer, (s) =>
            {
                using (Task<TransformResult> taskResult = pref.Request.Run(s.XbimModel, taskNode))
                {
                    taskResult.Wait();

                    switch (taskResult.Result.ResultCode)
                    {
                        case TransformResult.Code.Finished:
                            return new IfcStore(taskResult.Result.Target, s.FilePathName);
                        case TransformResult.Code.Canceled:
                            storeProducer.Logger?.DefaultLog.LogWarning("Canceled by user request ({0}).", s.FilePathName);
                            break;
                        case TransformResult.Code.ExitWithError:
                            storeProducer.Logger?.DefaultLog.LogError("Caught error ({0}): {1}", s.FilePathName, taskResult.Exception);
                            break;
                    }
                }
                return null;
            });
        }

    }
}
