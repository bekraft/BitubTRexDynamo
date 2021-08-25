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

        internal protected IfcModel(IfcModel ifcModel) 
            : base(ifcModel.Qualifier, ifcModel.Logger, ifcModel.GetActionLog())
        {
            Store = ifcModel.Store;            
        }

        internal protected IfcModel(IfcStore store, Qualifier qualifier, LogMessage[] propagateLog = null) 
            : base(qualifier, store.Logger, propagateLog)
        {
            Store = store;                        
        }

        internal protected IfcModel(IfcStore store) 
            : base(System.Guid.NewGuid().ToQualifier(), store.Logger)
        {
            Store = store;
        }

        protected override IfcModel RequalifyModel(Qualifier qualifier)
        {
            return new IfcModel(Store, qualifier, GetActionLog());
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
            return ifcModel?.Store.Logger;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new string FileName => base.FileName;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new string PathName => base.PathName;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new string FormatExtension => base.FormatExtension;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new string CanonicalFileName(string seperator = "-")
        {
            return base.CanonicalFileName(seperator);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new string CanonicalName(string seperator = "-")
        {
            return base.CanonicalName(seperator);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new IfcModel RelocatePath(string newPathName)
        {
            return base.RelocatePath(newPathName);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new IfcModel Rename(string fileNameWithoutExt)
        {
            return base.Rename(fileNameWithoutExt);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new IfcModel RenameWithReplacePattern(string replacePattern, string replaceWith)
        {
            return base.RenameWithReplacePattern(replacePattern, replaceWith);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new IfcModel RenameWithSuffix(string fragment)
        {
            return base.RenameWithSuffix(fragment);
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
        /// <param name="inModel">The IFC model to be saved</param>
        /// <param name="extension">The format extension to use</param>
        /// <param name="separator">The separator between name fragments</param>
        /// <returns>A log message</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IfcModel SaveAs(IfcModel inModel, string extension, string separator)
        {
            if (null == inModel)
                throw new ArgumentNullException("ifcModel");

            Logger logger = inModel.Store.Logger;
            IfcModel outModel;
            string filePathName;
            try
            {
                var internalModel = inModel.XbimModel;
                if (null == internalModel)
                    throw new ArgumentNullException("No internal model");

                outModel = inModel.ChangeFormat(extension);
                filePathName = outModel.GetFilePathName(separator, true);

                using (var fileStream = File.Create(filePathName))
                {
                    switch (extension.ToLower())
                    {
                        case "ifc":
                            internalModel.SaveAsIfc(fileStream, inModel.NotifySaveProgressChanged);
                            break;
                        case "ifcxml":
                            internalModel.SaveAsIfcXml(fileStream, inModel.NotifySaveProgressChanged);
                            break;
                        case "ifczip":
                            internalModel.SaveAsIfcZip(fileStream, Path.GetFileName(filePathName), Xbim.IO.StorageType.IfcZip | Xbim.IO.StorageType.Ifc, inModel.NotifySaveProgressChanged);
                            break;
                        default:
                            logger?.LogWarning("File extension not known: '{0}'. Use (IFC, IFCXML or IFCZIP)", Path.GetExtension(filePathName));
                            break;
                    }

                    fileStream.Close();
                }
                
                inModel.NotifyOnProgressEnded(LogReason.Saved, false, false);

                outModel.ActionLog.Add(new LogMessage(outModel.FileName, LogSeverity.Info, LogReason.Saved, "Saved '{0}'.", filePathName));
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Exception: {0}", e.Message);
                inModel.NotifyOnProgressEnded(LogReason.Saved, false, true);

                outModel = new IfcModel(inModel);
                outModel.ActionLog.Add(new LogMessage(outModel.FileName, LogSeverity.Error, LogReason.Saved, "Failure saving model."));
            }

            return outModel;
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
            return new IfcModel(Store, qualifier, GetActionLog());
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
        /// Lists all quantity sets defined by elements by their name.
        /// </summary>
        /// <returns>A list of property set names in use</returns>
        public string[] QuantitySetNames() => XbimModel?.Instances
            .OfType<IIfcQuantitySet>()
            .Where(e => e.DefinesType.Count() == 0)
            .Select(e => e.Name?.ToString())
            .Distinct()
            .ToArray();

        /// <summary>
        /// Lists all property sets defined by elements by their name.
        /// </summary>
        /// <returns>A list of property set names in use</returns>
        public string[] PropertySetNames() => XbimModel?.Instances
            .OfType<IIfcPropertySet>()
            .Where(e => e.DefinesType.Count() == 0)
            .Select(e => e.Name?.ToString())
            .Distinct()
            .ToArray();

        /// <summary>
        /// Lists all property sets defined by types by their name.
        /// </summary>
        /// <returns>A list of property set names in use</returns>
        public string[] PropertySetNamesOnTypes() => XbimModel?.Instances
            .OfType<IIfcPropertySet>()
            .Where(e => e.DefinesType.Count() > 0)
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
