using System;

using System.Collections.Generic;
using System.Linq;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;

using Bitub.Transfer;

using Store;
using Internal;

namespace Validation
{
    /// <summary>
    /// IFC GUID Temporary inmemory store.
    /// </summary>
    public class IfcGuidStore
    {
        #region Internals

        const string ID_SOURCE = "IfcRoot.GlobalId";
        const string ID_REASON = "Mandatory property";

        internal struct IfcInstanceRef
        {
            internal Qualifier Qualifier { get; set; }
            internal XbimInstanceHandle InstanceHandle { get; set; }
        }

        private IDictionary<string, List<IfcInstanceRef>> GuidStore = new Dictionary<string, List<IfcInstanceRef>>();
        private long _nextIndex = 0;

        private IfcGuidStore()
        {
        }

        internal IfcValidationMessage Put(Qualifier modelQualifier, IIfcRoot ifcObject)
        {
            if (null == ifcObject.GlobalId)
                return IfcValidationMessage.ByResult(_nextIndex++, null, IfcReportDomain.Schema, ID_SOURCE, "Not set", ID_REASON);

            List<IfcInstanceRef> refs;
            var @ref = new IfcInstanceRef { Qualifier = modelQualifier, InstanceHandle = new XbimInstanceHandle(ifcObject) };
            var ifcGuid = ifcObject.GlobalId.ToString();
            if (GuidStore.TryGetValue(ifcGuid, out refs))
            {
                refs.Add(@ref);
                return IfcValidationMessage.ByResult(
                    _nextIndex++, 
                    null, 
                    IfcReportDomain.SchemaConstraint, 
                    ID_SOURCE, "Not unique", ID_REASON, ifcObject.ExpressType.ExpressName,
                    ifcGuid);
            }
            else
            {
                GuidStore.Add(ifcGuid, new List<IfcInstanceRef>() { @ref });
                return IfcValidationMessage.ByResult(
                    _nextIndex++, 
                    null, 
                    IfcReportDomain.Passed, ID_SOURCE, "Set & unique", ID_REASON,
                    ifcGuid);
            }
        }

        #endregion

        /// <summary>
        /// An initiallly empty GUID store.
        /// </summary>
        /// <returns>The empty store</returns>
        public static IfcGuidStore ByEmptyStore()
        {
            return new IfcGuidStore();
        }

        /// <summary>
        /// Ingest a complete IFC model by querying it's IfcRoots GUID attributes.
        /// </summary>
        /// <param name="ifcModel">The IFC model</param>
        /// <returns>The modified store</returns>
        public IfcGuidStore Ingest(IfcModel ifcModel)
        {
            var innerModel = ifcModel.Store.XbimModel;
            if (null == innerModel)
                throw new ArgumentNullException(nameof(ifcModel));

            foreach (var instance in innerModel.Instances.OfType<IIfcRoot>())
            {
                Put(ifcModel.Qualifier, instance);
            }

            return this;
        }

        /// <summary>
        /// Returns a table of duplicate úsages of IfcGUIDs by model source, index label and EXPRESS type.
        /// </summary>
        /// <returns>A matrix of rows holding (IfcGUID, filename, label index and entity type)</returns>
        public object[][] ToDataOfDuplicateUsage()
        {
            return GuidStore
                .Where(g => g.Value.Count > 1)
                .SelectMany(g => g.Value.Select(v => new object[] { g.Key, IfcModel.GetFilePathName(v.Qualifier), v.InstanceHandle.EntityLabel, v.InstanceHandle.EntityExpressType.ExpressName }))
                .ToArray();
        }

        public int CountInstances() => GuidStore.Select(g => g.Value.Count).Sum();

        public int DuplicateUsageCountInstances() => GuidStore.Where(g => g.Value.Count > 1).Select(g => g.Value.Count).Sum();

        public int DuplicateUsageCount() => GuidStore.Where(g => g.Value.Count > 1).Count();
    }
}