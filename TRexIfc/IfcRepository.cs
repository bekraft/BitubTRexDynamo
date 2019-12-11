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
        internal ReportProgressDelegate ProgressDelegate { get; set; }

        internal IfcRepository()
        {
        }

        /// <summary>
        /// Reads and replaces the current internal model.
        /// </summary>
        /// <param name="fileName">File name to load</param>
        /// <returns>This instance</returns>
        [IsVisibleInDynamoLibrary(false)]
        public IfcRepository ReadFile(string fileName)
        {
            IfcStore.ModelProviderFactory.UseHeuristicModelProvider();
            XbimModel = IfcStore.Open(fileName, null, null, ProgressDelegate);
            return this;
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


        /// <summary>
        /// Creates a new repository using a progress feedback.
        /// </summary>
        /// <param name="progressReporter">The progress feedback</param>
        /// <returns>An opened IFC repository</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IfcRepository WithProgressReporter(IProgress<int> progressReporter)
        {
            return new IfcRepository { ProgressDelegate = (p, s) => progressReporter.Report(p) };
        }
    }
}
