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
        /// <param name="pref">The request preferences</param>
        /// <param name="storeProducer">The IFC store producer</param>
        /// <param name="taskNode">An optional task node for progress annoucement</param>
        /// <returns></returns>  
        [IsVisibleInDynamoLibrary(false)]
        public IIfcStoreProducer RemovePropertySets(PSetRemovalRequest pref, IIfcStoreProducer storeProducer, ICancelableTaskNode taskNode)
        {
            return new IfcStoreProducerDelegate(storeProducer, (s) =>
            {
                var task = pref.Request.Run(s.XbimModel, taskNode);
                task.Wait(TimeOut);

                using(var result = task.Result)
                {
                    switch (result.ResultCode)
                    {
                        case TransformResult.Code.Finished:
                            return new IfcStore(result.Target, storeProducer.Logger, s.FilePathName);
                        case TransformResult.Code.Canceled:
                            storeProducer.Logger?.LogWarning("Canceled by user request ({0}).", s.FilePathName);
                            break;
                        case TransformResult.Code.ExitWithError:
                            storeProducer.Logger?.LogError("Caught error ({0}): {1}", s.FilePathName, result.Cause);
                            break;
                    }
                }
                return null;
            });
        }

    }
}
