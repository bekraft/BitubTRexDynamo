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
                            return new IfcStore(taskResult.Result.Target, storeProducer.Logger, s.FilePathName);
                        case TransformResult.Code.Canceled:
                            storeProducer.Logger?.LogWarning("Canceled by user request ({0}).", s.FilePathName);
                            break;
                        case TransformResult.Code.ExitWithError:
                            storeProducer.Logger?.LogError("Caught error ({0}): {1}", s.FilePathName, taskResult.Exception);
                            break;
                    }
                }
                return null;
            });
        }

    }
}
