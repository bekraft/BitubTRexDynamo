using System;
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

namespace Store
{
    /// <summary>
    /// IFC model instance
    /// </summary>
    public class IfcModel : NodeProgressing, IProgress<ICancelableProgressState>
    {
#pragma warning disable CS1591

        #region Internals

        internal protected IfcModel(IfcStore store)
        {
            Store = store;
            (LogMessages as ObservableCollection<LogMessage>).CollectionChanged += (s, e) =>
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
            };
        }

        internal void NotifySaveProgressChanged(int percentage, object stateObject)
        {
            NotifyProgressChanged(ActionType.Save, percentage, stateObject);
        }

        internal void NotifyLoadProgressChanged(int percentage, object stateObject)
        {
            NotifyProgressChanged(ActionType.Load, percentage, stateObject);
        }

        internal protected void NotifyProgressChanged(ActionType action, int percentage, object stateObject)
        {
            OnProgressChanged(new NodeProgressingEventArgs(action, percentage, Name, stateObject));
        }

        internal protected void NotifyOnFinished(ActionType action, bool isCanceled, bool isBroken)
        {
            OnFinished(new NodeFinishedEventArgs(action, Name, isCanceled, isBroken));
        }

        #endregion

        [IsVisibleInDynamoLibrary(false)]
        public readonly IfcStore Store;

        [IsVisibleInDynamoLibrary(false)]
        public void Report(ICancelableProgressState value)
        {
            OnProgressChanged(new NodeProgressingEventArgs(ActionType.Change, value, Name));
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
        /// Returns the physical file name of the IFC model (present or not).
        /// </summary>
        public string Name { get => Store.FileName; }

        /// <summary>
        /// Gets the canoncial model name (depending on preceeding transformations).
        /// </summary>
        /// <param name="seperator">The separator between name fragments</param>
        public string CanonicalName(string seperator = "-") => Path.GetFileName(Store.GetFilePathName(true, seperator));

        /// <summary>
        /// Save an IFC model to file.
        /// </summary>
        /// <param name="ifcModel">The IFC model to be saved</param>
        /// <param name="extension">The format extension to use</param>
        /// <param name="usingCanonicalName">If true, use canonical name instead of simple name</param>
        /// <param name="separator">The separator between name fragments</param>
        /// <returns>A log message</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IfcModel SaveAs(IfcModel ifcModel, string extension, bool usingCanonicalName = true, string separator = "-")
        {
            var logger = ifcModel.Store.Logger;
            var filePathName = ifcModel.Store.GetFilePathName(usingCanonicalName, separator);
            var savedModel = new IfcModel(ifcModel.Store);
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
                logger?.LogInfo("Saved '{0}'.", filePathName);
                ifcModel.NotifyOnFinished(ActionType.Save, false, false);
                savedModel.LogMessages.Add(new LogMessage(Severity.Info, ActionType.Save, "Success: '{0}'.", filePathName));
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Caught exception: {0}", e.Message);
                ifcModel.NotifyOnFinished(ActionType.Save, false, true);
                savedModel.LogMessages.Add(new LogMessage(Severity.Error, ActionType.Load, "Failure: '{0}'.", filePathName));
            }

            return savedModel;
        }

        /// <summary>
        /// Relocates the containing folder.
        /// </summary>
        /// <param name="newPathName">The new path/folder name</param>
        /// <returns>A new reference to the model</returns>
        public IfcModel Relocate(string newPathName)
        {
            Store.Relocate(newPathName);
            return new IfcModel(Store);
        }

        /// <summary>
        /// Changes the format extension to "ifc", "ifcxml" or "ifczip".
        /// </summary>
        /// <param name="newExtension">The new format extension</param>
        /// <returns>A model reference</returns>
        public IfcModel SetFormat(string newExtension)
        {
            Store.ChangeFormat(newExtension);
            return new IfcModel(Store);
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

    }
}
