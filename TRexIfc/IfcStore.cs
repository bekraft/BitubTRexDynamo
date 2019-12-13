using Autodesk.DesignScript.Runtime;

using System.IO;
using System.Linq;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace TRexIfc
{
    /// <summary>
    /// An IFC repository.
    /// </summary>    
    public class IfcStore
    {
        #region Internals

        internal IModel XbimModel { get; set; }

        internal IfcStore()
        {
        }

        /// <summary>
        /// Reads and replaces the current internal model.
        /// </summary>
        /// <param name="fileName">File name to load</param>
        /// <param name="progressDelegate">The progress delegate</param>
        /// <returns>This instance</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IfcStore Load(string fileName, ReportProgressDelegate progressDelegate)
        {
            Xbim.Ifc.IfcStore.ModelProviderFactory.UseHeuristicModelProvider();
            return new IfcStore
            {
                XbimModel = Xbim.Ifc.IfcStore.Open(fileName, null, null, progressDelegate)
            };
        }

        /// <summary>
        /// Save an IFC model to file.
        /// </summary>
        /// <param name="fileName">The filename</param>
        /// <param name="store">The IFC store to be saved</param>
        /// <param name="progressDelegate">The progress delegate</param>
        [IsVisibleInDynamoLibrary(false)]
        public static void Save(string fileName, IfcStore store, ReportProgressDelegate progressDelegate)
        {
            using (var fileStream = File.Create(fileName))
            {
                store.XbimModel.SaveAsIfc(fileStream, progressDelegate);
                fileStream.Close();
            }
        }

        #endregion

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
