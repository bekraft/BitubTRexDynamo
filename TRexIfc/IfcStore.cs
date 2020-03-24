using Autodesk.DesignScript.Runtime;

using System;
using System.IO;
using System.Linq;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using TRexIfc.Logging;

namespace TRexIfc
{
    /// <summary>
    /// An IFC store bound to a physical resource.
    /// </summary>    
    public class IfcStore
    {
        #region Internals

        internal IModel XbimModel { get; private set; }
        internal string FilePathName { get; private set; }

        /// <summary>
        /// The logger.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public Logger Logger { get; set; }

        static IfcStore() {
            Xbim.Ifc.IfcStore.ModelProviderFactory.UseHeuristicModelProvider();
        }

        internal IfcStore(IModel model, string filePathName = null)
        {
            XbimModel = model;
            FilePathName = filePathName;
        }

        internal IfcStore(Xbim.Ifc.IfcStore ifcStore)
        {
            XbimModel = ifcStore;
            FilePathName = ifcStore.FileName;
        }

        /// <summary>
        /// Reads and replaces the current internal model.
        /// </summary>
        /// <param name="fileName">File name to load</param>
        /// <param name="logInstance">The logger instance</param>
        /// <param name="progressDelegate">The progress delegate</param>
        /// <returns>This instance</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IfcStore ByLoad(string fileName, Logger logInstance, ReportProgressDelegate progressDelegate)
        {
            XbimLogging.LoggerFactory = logInstance.LoggerFactory;
            var xbimStore = new IfcStore(Xbim.Ifc.IfcStore.Open(fileName, null, null, progressDelegate));
            return xbimStore;
        }

        /// <summary>
        /// Wraps a new instance around an exisiting Xbim store.
        /// </summary>
        /// <param name="ifcStore">The Xbim IFC store</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IfcStore ByXbimIfcStore(Xbim.Ifc.IfcStore ifcStore)
        {
            return new IfcStore(ifcStore);
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
            try
            {
                using (var fileStream = File.Create(fileName))
                {
                    switch (Path.GetExtension(fileName).ToLower())
                    {
                        case ".ifc":
                            store.XbimModel.SaveAsIfc(fileStream, progressDelegate);
                            break;
                        case ".ifcxml":
                            store.XbimModel.SaveAsIfcXml(fileStream, progressDelegate);
                            break;
                        case ".ifczip":
                            store.XbimModel.SaveAsIfcZip(fileStream, Path.GetFileName(fileName), Xbim.IO.StorageType.Ifc, progressDelegate);
                            break;
                        default:
                            store.Logger?.LogWarning("File extension not known: '{0}'. Use (IFC, IFCXML or IFCZIP)", Path.GetExtension(fileName));
                            break;
                    }

                    fileStream.Close();
                }
                store.Logger?.LogInfo("Saving '{0}' done.", fileName);
            }
            catch(Exception e)
            {
                store.Logger?.LogError("Caught exception: {0}", e.Message);
            }
        }

        #endregion

        /// <summary>
        /// Loads an IFC model by given name.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="logger">The optional logging instance.</param>
        /// <returns>An IFC model.</returns>
        public static IfcStore ByFileName(string fileName, Logger logger = null)
        {
            return ByLoad(fileName, logger, null);
        }

        /// <summary>
        /// The current IFC schema version of the element collection.
        /// </summary>
        public string Schema { get => XbimModel.SchemaVersion.ToString(); }

        /// <summary>
        /// The project name.
        /// </summary>
        public string ProjectName { get => XbimModel.Instances.FirstOrDefault<IIfcProject>()?.Name; }

        /// <summary>
        /// Returns the physical file name of the IFC model (present or not).
        /// </summary>
        public string FileName { get => Path.GetFileName(FilePathName); }

        /// <summary>
        /// Renames the current model's physical name. Does not rename the original
        /// physical resource. The change will only have effect when saving this store.
        /// </summary>
        /// <param name="fileNameWithExt">The new file name with extension</param>
        /// <returns>This store with new name</returns>
        public IfcStore Rename(string fileNameWithExt)
        {
            string path = Path.GetDirectoryName(FilePathName);
            FilePathName = $"{path}{Path.DirectorySeparatorChar}{fileNameWithExt}";
            return this;
        }

        /// <summary>
        /// Renames the current model's physical name append the given suffix.
        /// </summary>
        /// <param name="suffix">A suffix</param>
        /// <returns>This store with a new name</returns>
        public IfcStore RenameWithSuffix(string suffix)
        {
            return Rename($"{Path.GetFileNameWithoutExtension(FilePathName)}{suffix}{Path.GetExtension(FilePathName)}");
        }

        /// <summary>
        /// Saves the current store to a physical resource.
        /// </summary>
        /// <param name="fileNameWithExt">The file name with extension ("ifc", "ifcxml" or "ifczip")</param>
        /// <returns>The absolute file name including the path</returns>
        public string SaveAs(string fileNameWithExt)
        {
            Rename(fileNameWithExt);
            Save(FilePathName, this, null);
            return FilePathName;
        }

        /// <summary>
        /// Saves the current store to a physical resource by appending a suffix and using the
        /// given extension.
        /// </summary>
        /// <param name="addonSuffix">Some suffix</param>
        /// <param name="extension">The extension ("ifc", "ifcxml" or "ifczip")</param>
        /// <returns>The absolute file name including the path</returns>
        public string SaveAs(string addonSuffix, string extension)
        {
            return SaveAs($"{Path.GetFileNameWithoutExtension(FilePathName)}{addonSuffix}{extension}");
        }

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
