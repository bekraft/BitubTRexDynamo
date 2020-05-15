﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Autodesk.DesignScript.Runtime;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using Internal;
using Log;
using Bitub.Transfer;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Xbim.IO.Xml.BsConf;

namespace Store
{
    /// <summary>
    /// IFC model instance
    /// </summary>
    public class IfcModel : NodeProgressing, IProgress<ICancelableProgressState>, IDisposable
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
                        Guid = new Bitub.Transfer.Guid { Raw = System.Guid.NewGuid().ToByteArray().ToByteString() }
                    }
                };
            }
            else
            {
                Qualifier = qualifier;
            }

            (ActionLog as ObservableCollection<LogMessage>).CollectionChanged += ActionLog_CollectionChanged;
        }

        private void ActionLog_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (LogMessage msg in e.NewItems.Cast<LogMessage>())
                {
                    switch (msg.Severity)
                    {
                        case Severity.Info:
                            Store.Logger?.LogInfo(msg.Message);
                            break;
                        case Severity.Warning:
                            Store.Logger?.LogWarning(msg.Message);
                            break;
                        case Severity.Critical:
                        case Severity.Error:
                            Store.Logger?.LogError(msg.Message);
                            break;
                    }
                }
            }
        }

        internal void NotifySaveProgressChanged(int percentage, object stateObject)
        {
            NotifyProgressChanged(ActionType.Saved, percentage, stateObject);
        }

        internal void NotifyLoadProgressChanged(int percentage, object stateObject)
        {
            NotifyProgressChanged(ActionType.Loaded, percentage, stateObject);
        }

        internal protected void NotifyProgressChanged(ActionType action, int percentage, object stateObject)
        {
            OnProgressChanged(new NodeProgressingEventArgs(action, percentage, FileName, stateObject));
        }

        internal protected void NotifyOnFinished(ActionType action, bool isCanceled, bool isBroken)
        {
            OnFinished(new NodeFinishedEventArgs(action, FileName, isCanceled, isBroken));
        }

        [IsVisibleInDynamoLibrary(false)]
        public new void Report(ICancelableProgressState value)
        {
            OnProgressChanged(new NodeProgressingEventArgs(ActionType.Changed, value, Name));
        }

        public void Dispose()
        {
            if (null == Store) throw new ObjectDisposedException(nameof(IfcModel));
            (ActionLog as ObservableCollection<LogMessage>).CollectionChanged -= ActionLog_CollectionChanged;
            Store = null;
        }

        internal protected bool IsTemporaryModel
        {
            get => Qualifier.GuidOrNameCase != Qualifier.GuidOrNameOneofCase.Named;
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

        [IsVisibleInDynamoLibrary(false)]
        public string GetFilePathName(string canonicalSep = "-", bool withExtension = true)
        {            
            switch (Qualifier.GuidOrNameCase)
            {
                case Qualifier.GuidOrNameOneofCase.Anonymous:
                    var fileName1 = $"{Path.GetTempPath()}{Path.DirectorySeparatorChar}{Qualifier.Anonymous.ToBase64String()}";
                    return withExtension ? fileName1 + ".ifc" : fileName1;
                case Qualifier.GuidOrNameOneofCase.Named:
                    var fileName2 = string.IsNullOrEmpty(canonicalSep) ? Name : Qualifier.Named.ToLabel(canonicalSep, 1, 1);
                    var fullFileName2 = $"{Qualifier.Named.Frags[0]}{Path.DirectorySeparatorChar}{fileName2}";
                    return withExtension ?  $"{fullFileName2}.{Qualifier.Named.Frags.Last()}" : fullFileName2;
                default:
                    throw new NotSupportedException($"Missing qualifier");
            }
        }

#pragma warning restore CS1591

        /// <summary>
        /// The current IFC schema version of the element collection.
        /// </summary>
        public string Schema { get => Store.XbimModel.SchemaVersion.ToString(); }

        /// <summary>
        /// The project name.
        /// </summary>
        public string ProjectName { get => Store.XbimModel.Instances.FirstOrDefault<IIfcProject>()?.Name; }

        /// <summary>
        /// The source store name.
        /// </summary>
        public override string Name
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
        /// Returns the current log messages.
        /// </summary>
        /// <returns></returns>
        public LogMessage[] LogMessages { get => ActionLog.ToArray(); }

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
            var aboutToBeSaved = ifcModel.ChangeFormat(extension);
            var filePathName = aboutToBeSaved.GetFilePathName(separator, true);

            logger?.LogInfo("Saving '{0}'.", filePathName);
            try
            {
                using (var fileStream = File.Create(filePathName))
                {
                    switch (extension.ToLower())
                    {
                        case "ifc":
                            ifcModel.Store.XbimModel.SaveAsIfc(fileStream, ifcModel.NotifySaveProgressChanged);
                            break;
                        case "ifcxml":
                            ifcModel.Store.XbimModel.SaveAsIfcXml(fileStream, ifcModel.NotifySaveProgressChanged);
                            break;
                        case "ifczip":
                            ifcModel.Store.XbimModel.SaveAsIfcZip(fileStream, Path.GetFileName(filePathName), Xbim.IO.StorageType.Ifc, ifcModel.NotifySaveProgressChanged);
                            break;
                        default:
                            logger?.LogWarning("File extension not known: '{0}'. Use (IFC, IFCXML or IFCZIP)", Path.GetExtension(filePathName));
                            break;
                    }

                    fileStream.Close();
                }
                
                ifcModel.NotifyOnFinished(ActionType.Saved, false, false);
                aboutToBeSaved.ActionLog.Add(new LogMessage(Severity.Info, ActionType.Saved, "Success: '{0}'.", filePathName));
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Caught exception: {0}", e.Message);
                ifcModel.NotifyOnFinished(ActionType.Saved, false, true);
                aboutToBeSaved.ActionLog.Add(new LogMessage(Severity.Error, ActionType.Loaded, "Failure: '{0}'.", filePathName));
            }

            return aboutToBeSaved;
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
        public string[] ProductTypes() => Store.XbimModel.Instances
            .OfType<IIfcProduct>()
            .Select(p => p.ExpressType.Name)
            .Distinct()
            .ToArray();

        /// <summary>
        /// Lists all property sets by their name.
        /// </summary>
        /// <returns>A list of property set names in use</returns>
        public string[] PropertySetNames() => Store.XbimModel.Instances
            .OfType<IIfcPropertySet>()
            .Select(e => e.Name?.ToString())
            .Distinct()
            .ToArray();

        /// <summary>
        /// Graphical contexts
        /// </summary>
        /// <returns></returns>
        public string[] GraphicalContexts() => Store.XbimModel.Instances
                .OfType<IIfcGeometricRepresentationContext>()
                .Select(c => c.ContextIdentifier?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
    }
}