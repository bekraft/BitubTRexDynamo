using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using Bitub.Ifc.Scene;
using Bitub.Ifc.Transform.Requests;

using Google.Protobuf;

using Autodesk.DesignScript.Runtime;

namespace Data
{
    /// <summary>
    /// IFC aggregated helper utilities.
    /// </summary>
    public class DataCollector
    {
        #region Internal

        internal DataCollector()
        {
        }

        #endregion

        internal static string[] CollectPropertySets(Store.IfcStore ifcStore)
        {
            return ifcStore.XbimModel.Instances
                        .OfType<IIfcPropertySetDefinition>()
                        .Select(s => s.Name?.ToString())
                        .Distinct()
                        .ToArray();
        }

        /// <summary>
        /// Gets all property set names in distinct order.
        /// </summary>
        /// <param name="storeProducer">IFC model producer</param>
        /// <returns>A unique sequence of property set names</returns>
        public static string[][] CollectPropertySetNames(Store.IfcStoreProducer storeProducer)
        {
            List<string[]> namesPerModel = new List<string[]>();
            foreach(var store in storeProducer)
            {
                namesPerModel.Add(CollectPropertySets(store));
            }

            return namesPerModel.ToArray();
        }        
    }
}
