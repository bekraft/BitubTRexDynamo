using Autodesk.DesignScript.Runtime;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using Bitub.Ifc.Transform.Requests;

namespace TRexIfc
{
    /// <summary>
    /// An IFC repository.
    /// </summary>    
    public class IfcRepository
    {
        #region Internals

        internal IModel XbimModel { get; set; }

        internal IfcRepository()
        {
        }

        #endregion

        /// <summary>
        /// Load an IFC model from file.
        /// </summary>
        /// <param name="fileName">IFC file (ifc, ifcxml, ifczip)</param>
        /// <returns>An element collection</returns>        
        public static IfcRepository ByFileName(string fileName)
        {
            IfcStore.ModelProviderFactory.UseHeuristicModelProvider();
            IModel model = IfcStore.Open(fileName);
            return new IfcRepository { XbimModel = model };
        }

        /// <summary>
        /// Save an IFC model to file.
        /// </summary>
        /// <param name="fileName">The filename</param>
        /// <param name="collection">The IFC element collection to be saved</param>
        public static void Save(string fileName, IfcRepository collection)
        {
            using (var fileStream = File.Create(fileName))
            {
                collection.XbimModel.SaveAsIfc(fileStream);
                fileStream.Close();
            }
        }

        /// <summary>
        /// The current IFC schema version of the element collection.
        /// </summary>
        public string Schema() => XbimModel.SchemaVersion.ToString();

        /// <summary>
        /// Lists all product types which are present in the model
        /// </summary>
        /// <returns>A list of distinct IFC product types held by the collection</returns>
        public string[] ListProductTypes() => XbimModel.Instances
            .OfType<IIfcProduct>()
            .Select(p => p.ExpressType.Name)
            .Distinct()
            .ToArray();

        /// <summary>
        /// Lists all property sets by their name.
        /// </summary>
        /// <returns>A list of property set names in use</returns>
        public string[] ListPropertySetNames() => XbimModel.Instances
            .OfType<IIfcPropertySet>()
            .Select(e => (string)e.Name)
            .Distinct()
            .ToArray();

    }
}
