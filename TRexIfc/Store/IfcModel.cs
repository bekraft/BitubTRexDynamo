using System;
using System.IO;
using System.Linq;

using System.Collections.Generic;

using Autodesk.DesignScript.Runtime;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using Bitub.Ifc;
using Bitub.Dto;

using TRex.Internal;
using TRex.Log;

namespace TRex.Store
{
    /// <summary>
    /// IFC model instance
    /// </summary>
    public class IfcModel : ProgressingModelTask<IfcModel>
    {
#pragma warning disable CS1591

        #region Internals

        internal protected IfcModel(IfcModel ifcModel) : base(ifcModel.Qualifier, ifcModel.Logger)
        {
            Store = ifcModel.Store;            
        }

        internal protected IfcModel(IfcStore store, Qualifier qualifier) : base(qualifier, store.Logger)
        {
            Store = store;            
        }

        internal protected IfcModel(IfcStore store) : base(System.Guid.NewGuid().ToQualifier(), store.Logger)
        {
            Store = store;
        }

        protected override IfcModel RequalifyModel(Qualifier qualifier)
        {
            return new IfcModel(Store, qualifier);
        }

        internal void NotifySaveProgressChanged(int percentage, object stateObject)
        {
            NotifyOnProgressChanged(LogReason.Saved, percentage, stateObject);
        }

        internal void NotifyLoadProgressChanged(int percentage, object stateObject)
        {
            NotifyOnProgressChanged(LogReason.Loaded, percentage, stateObject);
        }

        internal IModel XbimModel
        {
            get => IsCanceled ? null : Store.XbimModel;
        }

        #endregion

        [IsVisibleInDynamoLibrary(false)]
        public IfcStore Store { get; private set; }

        [IsVisibleInDynamoLibrary(false)]
        public static Logger GetLogger(IfcModel ifcModel)
        {
            return ifcModel.Store.Logger;
        }

#pragma warning restore CS1591

        /// <summary>
        /// The current IFC schema version of the element collection.
        /// </summary>
        public string Schema { get => XbimModel?.SchemaVersion.ToString(); }

        /// <summary>
        /// The project name.
        /// </summary>
        public string ProjectName { get => XbimModel?.Instances.FirstOrDefault<IIfcProject>()?.Name; }

        /// <summary>
        /// Save an IFC model to file.
        /// </summary>
        /// <param name="ifcModel">The IFC model to be saved</param>
        /// <param name="extension">The format extension to use</param>
        /// <param name="separator">The separator between name fragments</param>
        /// <returns>A log message</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IfcModel SaveAs(IfcModel ifcModel, string extension, string separator = "-")
        {
            if (null == ifcModel)
                throw new ArgumentNullException("ifcModel");

            var logger = ifcModel.Store.Logger;                       
            var savingModel = ifcModel.ChangeFormat(extension);
            var filePathName = savingModel.GetFilePathName(separator, true);

            try
            {
                var internalModel = ifcModel.XbimModel;
                if (null == internalModel)
                    throw new ArgumentNullException("No internal model");

                using (var fileStream = File.Create(filePathName))
                {
                    switch (extension.ToLower())
                    {
                        case "ifc":
                            internalModel.SaveAsIfc(fileStream, ifcModel.NotifySaveProgressChanged);
                            break;
                        case "ifcxml":
                            internalModel.SaveAsIfcXml(fileStream, ifcModel.NotifySaveProgressChanged);
                            break;
                        case "ifczip":
                            internalModel.SaveAsIfcZip(fileStream, Path.GetFileName(filePathName), Xbim.IO.StorageType.IfcZip | Xbim.IO.StorageType.Ifc, ifcModel.NotifySaveProgressChanged);
                            break;
                        default:
                            logger?.LogWarning("File extension not known: '{0}'. Use (IFC, IFCXML or IFCZIP)", Path.GetExtension(filePathName));
                            break;
                    }

                    fileStream.Close();
                }
                
                ifcModel.NotifyOnProgressEnded(LogReason.Saved, false, false);
                savingModel.ActionLog.Add(new LogMessage(savingModel.FileName, LogSeverity.Info, LogReason.Saved, "Saved '{0}'.", filePathName));
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Exception: {0}", e.Message);
                ifcModel.NotifyOnProgressEnded(LogReason.Saved, false, true);
                savingModel.ActionLog.Add(new LogMessage(savingModel.FileName, LogSeverity.Error, LogReason.Saved, "Failure: '{0}'.", filePathName));
            }

            return savingModel;
        }

        /// <summary>
        /// Changes the format extension identifier.
        /// </summary>
        /// <param name="newExtension">Either .ifc, .ifcxml or .ifczip</param>
        public IfcModel ChangeFormat(string newExtension)
        {
            if (IsTemporaryModel)
                throw new NotSupportedException("Not allowed for temporary models");

            var ext = IfcStore.Extensions.First(e => e.Equals(newExtension, StringComparison.OrdinalIgnoreCase));
            var qualifier = new Qualifier(Qualifier);
            qualifier.Named.Frags[Qualifier.Named.Frags.Count - 1] = ext;
            return new IfcModel(Store, qualifier);
        }

        /// <summary>
        /// Lists all product types which are present in the model
        /// </summary>
        /// <returns>A list of distinct IFC product types held by the collection</returns>
        public string[] ProductTypes() => XbimModel?.Instances
            .OfType<IIfcProduct>()
            .Select(p => p.ExpressType.Name)
            .Distinct()
            .ToArray();

        /// <summary>
        /// Lists all property sets by their name.
        /// </summary>
        /// <returns>A list of property set names in use</returns>
        public string[] PropertySetNames() => XbimModel?.Instances
            .OfType<IIfcPropertySetDefinition>()
            .Select(e => e.Name?.ToString())
            .Distinct()
            .ToArray();

        /// <summary>
        /// Gets all property names from model.
        /// </summary>
        /// <returns>The property names</returns>
        public string[] PropertyNames() => XbimModel?.Instances
            .OfType<IIfcProperty>()
            .Select(p => p.Name.ToString())
            .Distinct()
            .ToArray();

        /// <summary>
        /// Gets all full property names (set name and property name) from model.
        /// </summary>
        /// <param name="delimiter">The delimiter between set and property name ("." by default)</param>
        /// <returns>The property names</returns>
        public string[] FullPropertyNames(string delimiter = ".") => XbimModel?.Instances
            .OfType<IIfcProperty>()
            .SelectMany(p => p.PartOfPset.Select(s => $"{s.Name}{delimiter}{p.Name}"))
            .Distinct()
            .ToArray();

        /// <summary>
        /// Lists all property sets by declaring product type.
        /// </summary>
        /// <returns>A list of property set names in use</returns>
        public Dictionary<string, string[]> PropertySetsPerProductType() => XbimModel?.Instances
            .OfType<IIfcPropertySetDefinition>()
            .SelectMany(s => s.DefinesOccurrence.SelectMany(r => r.RelatedObjects.OfType<IIfcProduct>().Select(e => (e.ExpressType.Name, s.Name?.ToString()))))
            .ToLookup(t => t.Item1, t => t.Item2)
            .ToDictionary(g => g.Key, g => g.Distinct().ToArray());

        /// <summary>
        /// Returns a dictionary of pset names referencing property names.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string[]> PropertiesPerSet() => XbimModel?.Instances
            .OfType<IIfcProperty>()
            .SelectMany(p => p.PartOfPset.Select(s => (s.Name, p.Name)))
            .ToLookup(p => p.Item1, p => p.Item2.ToString())
            .ToDictionary(p => p.Key?.ToString(), p => p.Distinct().ToArray());

        /// <summary>
        /// Returns property values per IfcGloballyUniqueId.
        /// </summary>
        /// <returns>A dictionary lead by GUID.</returns>
        public Dictionary<string, string[]> PropertyValuePerObject() =>
            throw new NotImplementedException();

        /// <summary>
        /// Graphical contexts
        /// </summary>
        /// <returns></returns>
        public string[] GraphicalContexts() => XbimModel?.Instances
                .OfType<IIfcGeometricRepresentationContext>()
                .Select(c => c.ContextIdentifier?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
    }
}
