using Autodesk.DesignScript.Runtime;
using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using System.Threading;
using System.Collections.Concurrent;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using Log;

namespace Store
{
    /// <summary>
    /// An IFC store bound to a physical resource.
    /// </summary>    
    public class IfcStore : IDisposable
    {
        #region Internals

        // Global registry
        private static ConcurrentDictionary<string, IfcStore> IfcStoreRegistry;
        
        /// <summary>
        /// The logger instance (or null if there is no).
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public Logger Logger { get; set; }

        /// <summary>
        /// The wrapped internal IFC model.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public IModel XbimModel { get; private set; }

        /// <summary>
        /// The full path file name.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public string FilePathName { get; private set; }

        /// <summary>
        /// Send if the store is finally about to be disposed.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public event EventHandler OnDisposed;

        static IfcStore() {
            IfcStoreRegistry = new ConcurrentDictionary<string, IfcStore>();
        }

        internal IfcStore(IModel model, Logger logInstance, string filePathName = null)
        {
            XbimModel = model;
            FilePathName = filePathName;
        }

        internal IfcStore(Xbim.Ifc.IfcStore ifcStore, Logger logInstance)
        {
            XbimModel = ifcStore;
            FilePathName = ifcStore.FileName;
            Logger = logInstance;
        }

        /// <summary>
        /// Initializes the logging instance.
        /// </summary>
        /// <param name="logInstance"></param>
        [IsVisibleInDynamoLibrary(false)]
        public static void InitLogging(Logger logInstance)
        {
            XbimLogging.LoggerFactory = logInstance?.LoggerFactory ?? new LoggerFactory();
            Xbim.Ifc.IfcStore.ModelProviderFactory = new DefaultModelProviderFactory();
            Xbim.Ifc.IfcStore.ModelProviderFactory.UseHeuristicModelProvider();            
        }

        /// <summary>
        /// Reads and replaces the current internal model. Will also update the logger.
        /// </summary>
        /// <param name="fileName">File name to load</param>
        /// <param name="logInstance">The logger instance</param>
        /// <param name="progressDelegate">The progress delegate</param>
        /// <returns>This instance</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IfcStore ByInitAndLoad(string fileName, Logger logInstance, ReportProgressDelegate progressDelegate)
        {
            InitLogging(logInstance);            
            return ByLoad(fileName, logInstance, progressDelegate);
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
            // Load or return existing
            return IfcStoreRegistry.AddOrUpdate(fileName, (f) => ByPhysicallyLoad(f, logInstance, progressDelegate), (f,s) => s);
        }

        // Really loads store
        private static IfcStore ByPhysicallyLoad(string fileName, Logger logInstance, ReportProgressDelegate progressDelegate)
        {
            logInstance?.LogInfo("Start loading file '{0}'.", fileName);
            try
            {
                var xbimStore = new IfcStore(Xbim.Ifc.IfcStore.Open(fileName, null, null, progressDelegate, Xbim.IO.XbimDBAccess.Read), logInstance);
                logInstance?.LogInfo("File '{0}' has been loaded successfully.", fileName);                
                return xbimStore;
            }
            catch(Exception e)
            {
                logInstance?.LogError(e, "Exception while loading '{0}'.", fileName);
            }
            return null;
        }


        /// <summary>
        /// Wraps a new instance around an exisiting Xbim store.
        /// </summary>
        /// <param name="ifcStore">The Xbim IFC store</param>
        /// <param name="logInstance">The logging instance</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IfcStore ByXbimIfcStore(Xbim.Ifc.IfcStore ifcStore, Logger logInstance)
        {
            return new IfcStore(ifcStore, logInstance);
        }

        /// <summary>
        /// Save an IFC model to file.
        /// </summary>
        /// <param name="fileName">The filename</param>
        /// <param name="newFileExtension">Change format and extension to given extension (or leave it if <c>null</c>)</param>
        /// <param name="store">The IFC store to be saved</param>
        /// <param name="progressDelegate">The progress delegate</param>
        [IsVisibleInDynamoLibrary(false)]
        public static string Save(string fileName, string newFileExtension, IfcStore store, ReportProgressDelegate progressDelegate)
        {
            try
            {
                fileName = ChangeExtension(fileName, newFileExtension);
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
                return fileName;
            }
            catch(Exception e)
            {
                store.Logger?.LogError(e, "Caught exception: {0}", e.Message);
                throw e;
            }            
        }

        /// <summary>
        /// Saves the current store by current file name.
        /// </summary>
        /// <param name="store">The IFC store</param>
        /// <param name="newFileExtension">An optionally given new extension</param>
        /// <param name="progressDelegate">The progress delegate</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string Save(IfcStore store, string newFileExtension, ReportProgressDelegate progressDelegate)
        {
            return Save(store.FilePathName, newFileExtension, store, progressDelegate);
        }

        /// <summary>
        /// Changes the current file name extension.
        /// </summary>
        /// <param name="filePathName">The current file path name</param>
        /// <param name="newExtension">The new extension (using a dot as first character)</param>
        [IsVisibleInDynamoLibrary(false)]
        public static string ChangeExtension(string filePathName, string newExtension)
        {
            if (!string.IsNullOrWhiteSpace(newExtension))
            {
                string ext = !newExtension.Trim().StartsWith(".") ? $".{newExtension.Trim()}" : newExtension.Trim();
                filePathName = $"{Path.GetDirectoryName(filePathName)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(filePathName)}{ext}";
            }
            return filePathName;
        }

        /// <summary>
        /// Changes the format extension identifier.
        /// </summary>
        /// <param name="newExtension">Either .ifc, .ifcxml or .ifczip</param>
        /// <returns>This store with a new physical format extension</returns>
        public IfcStore ChangeExtension(string newExtension)
        {
            FilePathName = ChangeExtension(FilePathName, newExtension);
            return this;
        }

        #endregion

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
            Logger?.LogInfo("Renamed '{0}' to '{1}'.", Path.GetFileName(FilePathName), fileNameWithExt);
            FilePathName = $"{path}{Path.DirectorySeparatorChar}{fileNameWithExt}";            
            return this;
        }

        /// <summary>
        /// Relocates the file to another folder / directory.
        /// </summary>
        /// <param name="pathName">The absolute path name</param>
        /// <returns>This store having a relocated file name</returns>
        public IfcStore Relocate(string pathName)
        {            
            string fileName = Path.GetFileName(FilePathName);
            FilePathName = $"{pathName}{Path.DirectorySeparatorChar}{fileName}";
            Logger?.LogInfo("Relocated '{0}' to '{1}'.", fileName, FilePathName);
            return this;
        }

        /// <summary>
        /// Replaces fragments of the file name (without extension) by given fragments
        /// </summary>
        /// <param name="replacePattern">Regular expression identifiying the replacement</param>
        /// <param name="replaceWith">Fragments to insert</param>
        /// <returns>A store with modified filename</returns>
        public IfcStore RenameWithReplacePattern(string replacePattern, string replaceWith)
        {
            var modifiedName = Regex.Replace(
                    Path.GetFileNameWithoutExtension(FilePathName),
                    replacePattern,
                    replaceWith).Trim();            
            return Rename($"{modifiedName}{Path.GetExtension(FilePathName)}");
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
        /// Saves the current model to file.
        /// </summary>
        /// <returns>The full path name of saved file.</returns>
        public string Save()
        {
            Save(FilePathName, null, this, null);
            return FilePathName;
        }

        /// <summary>
        /// Saves the current store to a physical resource.
        /// </summary>
        /// <param name="fileNameWithExt">The file name with extension ("ifc", "ifcxml" or "ifczip")</param>
        /// <returns>The absolute file name including the path</returns>
        public string SaveAs(string fileNameWithExt)
        {
            Rename(fileNameWithExt);
            Save(FilePathName, null, this, null);
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

        /// <summary>
        /// Disposes the store object and the model internally.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public void Dispose()
        {
            IfcStore store;
            if (IfcStoreRegistry.TryRemove(FilePathName, out store))
            {
                if (store == this)
                {
                    XbimModel?.Dispose();
                    XbimModel = null;
                    OnDisposed?.Invoke(this, new EventArgs());
                }
                else
                {
                    throw new NotSupportedException($"Invalid state in global store registry: Duplicate '{FilePathName}'");
                }
            }
        }
    }
}
