using System;
using System.Text.RegularExpressions;

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

        internal static IIfcStoreProducer MapTo(IIfcStoreProducer source, Func<IfcStore,IfcStore> delegateFunction, ICancelableTaskNode taskNode)
        {

        }

        #endregion

        [IsVisibleInDynamoLibrary(false)]
        public static IfcStore RemovePropertySets(PSetRemovalRequest pref, IfcStore ifcStore, ICancelableTaskNode taskNode)
        {
            foreach(var ifcStore in new IfcStoreSeq(storeProducer))
            {
                var task = pref.Request.Run(ifcStore.XbimModel, null);
                task.Wait();

                switch(task.Result.ResultCode)
                {
                    case Bitub.Ifc.Transform.TransformResult.Code.Finished:
                        task.Result.Target
                }

            if (task.Result.ResultCode == Bitub.Ifc.Transform.TransformResult.Code.Finished)
                {
                    var transformedModel = task.Result.Target;
                }

            }

        }

        public static IIfcStoreProducer RemovePropertySets(PSetRemovalRequest pref, IIfcStoreProducer storeProducer)
        {
            return null;
        }

    }
}
