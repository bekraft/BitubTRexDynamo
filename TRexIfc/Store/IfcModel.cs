using System;
using System.IO;
using System.Linq;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

using Autodesk.DesignScript.Runtime;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using Bitub.Ifc;
using Bitub.Dto;

using Internal;
using Log;

namespace Store
{
    /// <summary>
    /// IFC model instance
    /// </summary>
    public class IfcModel : ProgressingTask, IDisposable
    {
#pragma warning disable CS1591

        #region Internals

        internal protected IfcModel(IfcModel ifcModel)
        {
            Store = ifcModel.Store;
            Qualifier = ifcModel.Qualifier;
        }

        internal protected IfcModel(IfcStore store, Qualifier qualifier)
        {
            Store = store;
            if (null == qualifier)
            {
                Qualifier = new Qualifier
                {
                    Anonymous = new GlobalUniqueId
                    {
                        Guid = new Bitub.Dto.Guid { Raw = System.Guid.NewGuid().ToByteArray().ToByteString() }
                    }
                };
            }
            else
            {
                Qualifier = qualifier;
            }

            ActionLog.CollectionChanged += ActionLog_CollectionChanged;
        }

        private void ActionLog_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (LogMessage msg in e.NewItems.Cast<LogMessage>())
                {
                    switch (msg.Severity)
                    {
                        case LogSeverity.Debug:
                        case LogSeverity.Info:
                            Store.Logger?.LogInfo(msg.ToString());
                            break;
                        case LogSeverity.Warning:
                            Store.Logger?.LogWarning(msg.ToString());
                            break;
                        case LogSeverity.Critical:
                        case LogSeverity.Error:
                            Store.Logger?.LogError(msg.ToString());
                            break;
                    }
                }
            }
        }

        internal void NotifySaveProgressChanged(int percentage, object stateObject)
        {
            NotifyOnProgressChanged(LogReason.Saved, percentage, stateObject);
        }

        internal void NotifyLoadProgressChanged(int percentage, object stateObject)
        {
            NotifyOnProgressChanged(LogReason.Loaded, percentage, stateObject);
        }

        [IsVisibleInDynamoLibrary(false)]
        public override void Dispose()
        {
            if (null == Store)
                throw new ObjectDisposedException(nameof(IfcModel));

            base.Dispose();

            ActionLog.CollectionChanged -= ActionLog_CollectionChanged;
            Store = null;
        }

        internal protected bool IsTemporaryModel
        {
            get => Qualifier.GuidOrNameCase != Qualifier.GuidOrNameOneofCase.Named;
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

        [IsVisibleInDynamoLibrary(false)]
        public Qualifier Qualifier { get; private set; }

        internal static string GetFilePathName(Qualifier qualifier, string canonicalSep = "-", bool withExtension = true)
        {
            switch (qualifier.GuidOrNameCase)
            {
                case Qualifier.GuidOrNameOneofCase.Anonymous:
                    var fileName1 = $"{Path.GetTempPath()}{Path.DirectorySeparatorChar}{qualifier.Anonymous.ToBase64String()}";
                    return withExtension ? fileName1 + ".ifc" : fileName1;
                case Qualifier.GuidOrNameOneofCase.Named:
                    var fileName2 = string.IsNullOrEmpty(canonicalSep) ? qualifier.Named.Frags[1] : qualifier.Named.ToLabel(canonicalSep, 1, 1);
                    var fullFileName2 = $"{qualifier.Named.Frags[0]}{Path.DirectorySeparatorChar}{fileName2}";
                    return withExtension ? $"{fullFileName2}.{qualifier.Named.Frags.Last()}" : fullFileName2;
                default:
                    throw new NotSupportedException($"Missing qualifier");
            }
        }

        internal string GetFilePathName(string canonicalSep = "-", bool withExtension = true)
        {
            return GetFilePathName(Qualifier, canonicalSep, withExtension);
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
        /// The source store name.
        /// </summary>        
        internal override string Name
        {
            get {
                switch (Qualifier.GuidOrNameCase)
                {
                    case Qualifier.GuidOrNameOneofCase.Anonymous:
                        return Qualifier.Anonymous.ToBase64String();
                    case Qualifier.GuidOrNameOneofCase.Named:
                        return Qualifier.Named.Frags[1];
                    default:
                        throw new NotSupportedException($"Missing qualifier");
                }
            }
        }

        /// <summary>
        /// The assembled file name.
        /// </summary>
        public string FileName
        {
            get {
                switch (Qualifier.GuidOrNameCase)
                {
                    case Qualifier.GuidOrNameOneofCase.Anonymous:
                        return $"{Qualifier.Anonymous.ToBase64String()}.ifc";
                    case Qualifier.GuidOrNameOneofCase.Named:
                        return $"{Qualifier.Named.Frags[1]}.{Qualifier.Named.Frags.Last()}";
                    default:
                        throw new NotSupportedException($"Missing qualifier");
                }
            }
        }

        /// <summary>
        /// The resource path name of physical store.
        /// </summary>
        public string PathName
        {
            get {
                switch (Qualifier.GuidOrNameCase)
                {
                    case Qualifier.GuidOrNameOneofCase.Anonymous:
                        return Path.GetTempPath();
                    case Qualifier.GuidOrNameOneofCase.Named:
                        return Qualifier.Named.Frags[0];
                    default:
                        throw new NotSupportedException($"Missing qualifier");
                }
            }
        }

        /// <summary>
        /// The format extension ("ifc", "ifcxml" or "ifczip")
        /// </summary>
        public string FormatExtension
        {
            get {
                switch (Qualifier.GuidOrNameCase)
                {
                    case Qualifier.GuidOrNameOneofCase.Anonymous:
                        return "ifc";
                    case Qualifier.GuidOrNameOneofCase.Named:
                        return Qualifier.Named.Frags.Last();
                    default:
                        throw new NotSupportedException($"Missing qualifier");
                }
            }
        }

        /// <summary>
        /// Gets the canoncial model name (depending on preceeding transformations).
        /// </summary>
        /// <param name="seperator">The separator between name fragments</param>
        public string CanonicalName(string seperator = "-") => Path.GetFileName(GetFilePathName(seperator, false));

        /// <summary>
        /// Gets the canoncial model file name (depending on preceeding transformations).
        /// </summary>
        /// <param name="seperator">The separator between name fragments</param>
        public string CanonicalFileName(string seperator = "-") => Path.GetFileName(GetFilePathName(seperator, true));

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

            if (ifcModel.IsCanceled)
            {
                savingModel.IsCanceled = true;
                savingModel.ActionLog.Add(
                    LogMessage.BySeverityAndMessage(savingModel.Name, LogSeverity.Info, LogReason.Saved, "Saving '{0}' has been canceled.", filePathName));
                return savingModel;
            }

            try
            {
                var internalModel = ifcModel.XbimModel;
                if (null == internalModel)
                    throw new Exception("No internal model");

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
                savingModel.ActionLog.Add(new LogMessage(savingModel.FileName, LogSeverity.Error, LogReason.Loaded, "Failure: '{0}'.", filePathName));
            }

            return savingModel;
        }

        /// <summary>
        /// Renames the current model's physical name. Does not rename the original
        /// physical resource. The change will only have effect when saving this store.
        /// </summary>
        /// <param name="fileNameWithoutExt">The new file name without format extension</param>
        public IfcModel Rename(string fileNameWithoutExt)
        {
            Qualifier qualifier;
            if (IsTemporaryModel)
            {
                Store.Logger?.LogInfo($"Renaming temporary store '{Qualifier.Anonymous.ToBase64String()}'");
                qualifier = IfcStore.BuildQualifier("~", fileNameWithoutExt, "ifc");
            }
            else
            {
                qualifier = new Qualifier(Qualifier);
            }

            qualifier.Named.Frags[1] = fileNameWithoutExt;
            return new IfcModel(Store, qualifier);
        }

        /// <summary>
        /// Relocates the containing folder.
        /// </summary>
        /// <param name="newPathName">The new path/folder name</param>
        /// <returns>A new reference to the model</returns>
        public IfcModel RelocatePath(string newPathName)
        {
            Qualifier qualifier;
            if (IsTemporaryModel)
            {
                Store.Logger?.LogInfo($"Relocating temporary store '{Qualifier.Anonymous.ToBase64String()}'");
                qualifier = IfcStore.BuildQualifier(newPathName, Qualifier.Anonymous.ToBase64String(), "ifc");
            }
            else
            {
                qualifier = new Qualifier(Qualifier);
            }

            qualifier.Named.Frags[0] = Path.GetDirectoryName($"{newPathName}{Path.DirectorySeparatorChar}");

            return new IfcModel(Store, qualifier);
        }

        /// <summary>
        /// Replaces fragments of the file name (without extension) by given fragments
        /// </summary>
        /// <param name="replacePattern">Regular expression identifiying the replacement</param>
        /// <param name="replaceWith">Fragments to insert</param>
        public IfcModel RenameWithReplacePattern(string replacePattern, string replaceWith)
        {
            if (IsTemporaryModel)
                throw new NotSupportedException("Not allowed for temporary models");
            
            var modifiedName = Regex.Replace(Qualifier.Named.Frags[1], replacePattern, replaceWith).Trim();
            return Rename(modifiedName);
        }

        /// <summary>
        /// Renames the current model's physical name append the given suffix.
        /// </summary>
        /// <param name="suffix">A suffix</param>
        public IfcModel RenameWithSuffix(string suffix)
        {
            if (IsTemporaryModel)
                throw new NotSupportedException("Not allowed for temporary models");

            var qualifier = new Qualifier(Qualifier);
            qualifier.Named.Frags.Insert(qualifier.Named.Frags.Count - 1, suffix);
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
