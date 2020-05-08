using Autodesk.DesignScript.Runtime;
using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using System.Threading;
using System.Collections.Concurrent;

using Bitub.Transfer;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using System.Collections.Generic;
using System.Collections.ObjectModel;

using Log;
using Internal;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Store
{
    /// <summary>
    /// Simple IFC model producer.
    /// </summary>
    /// <returns></returns>
    [IsVisibleInDynamoLibrary(false)]
    public delegate IModel ProducerDelegate();

    /// <summary>
    /// Delegating model production.
    /// </summary>
    /// <param name="node">The log and progress receiver</param>
    /// <returns></returns>
    [IsVisibleInDynamoLibrary(false)]
    public delegate IModel ProducerProgressDelegate(INodeProgressing node);

    /// <summary>
    /// Delgating model transformation.
    /// </summary>
    /// <param name="sourceModel">The source model</param>
    /// <param name="node">The log and progress receiver</param>
    /// <returns></returns>
    [IsVisibleInDynamoLibrary(false)]
    public delegate IModel TransformerProgressDelegate(IModel sourceModel, INodeProgressing node);

    /// <summary>
    /// Background IFC storage handling.
    /// </summary> 
    [IsVisibleInDynamoLibrary(false)]
    public class IfcStore
    {
        /// <summary>
        /// Available IFC physical file format extensions.
        /// </summary>
        public static string[] Extensions = new string[] { "ifc", "ifczip", "ifcxml" };

        // Disable comment warning
#pragma warning disable CS1591

        #region Internals

        // The internal weak reference
        private WeakReference<IModel> _model;
        // The model producer hook 
        private ProducerDelegate _producer;

        static IfcStore()
        {
            Xbim.Ifc.IfcStore.ModelProviderFactory = new DefaultModelProviderFactory();
            Xbim.Ifc.IfcStore.ModelProviderFactory.UseHeuristicModelProvider();
        }

        private IfcStore(Qualifier qualifier)
        {
            if (null == qualifier)
                Qualifier = new Qualifier
                {
                    Anonymous = new GlobalUniqueId
                    {
                        Guid = new Bitub.Transfer.Guid { Raw = System.Guid.NewGuid().ToByteArray().ToByteString() }
                    }
                };
            else
                Qualifier = qualifier;
        }

        private IfcStore(IModel model, Qualifier qualifier = null) 
            : this(qualifier)
        {
            XbimModel = model;
        }

        private IfcStore(IModel model, string canonicalName, Qualifier sourceQualifier = null) 
            : this(BuildQualifier(sourceQualifier, canonicalName))
        {
            XbimModel = model;
        }

        private IfcStore(ProducerDelegate producerDelegate, string canonicalName, Qualifier sourceQualifier = null)
            : this(BuildQualifier(sourceQualifier, canonicalName))
        {
            _producer = producerDelegate;
        }

        internal static Qualifier BuildQualifier(string pathName, string fileName, string format)
        {
            var name = new Name();
            var ext = Extensions.First(e => e.Equals(format.ToLower(), StringComparison.OrdinalIgnoreCase));

            name.Frags.AddRange(new string[] { pathName, fileName, ext });
            return new Qualifier
            {
                Named = name
            };
        }

        internal static Qualifier BuildQualifier(string filePathName)
        {
            return BuildQualifier(
                Path.GetDirectoryName(filePathName),
                Path.GetFileNameWithoutExtension(filePathName),
                Path.GetExtension(filePathName).Substring(1));
        }

        internal static Qualifier BuildQualifier(Qualifier sourceQualifier, string canonicalName)
        {
            if (null == sourceQualifier)
                return BuildQualifier(canonicalName);

            Qualifier newQualifier;
            switch (sourceQualifier.GuidOrNameCase)
            {
                case Qualifier.GuidOrNameOneofCase.Anonymous:
                    throw new ArgumentException($"Source is temporary model qualifier");
                case Qualifier.GuidOrNameOneofCase.None:
                    newQualifier = BuildQualifier(canonicalName);
                    break;
                case Qualifier.GuidOrNameOneofCase.Named:
                    newQualifier = new Qualifier(sourceQualifier);
                    newQualifier.Named.Frags.Insert(newQualifier.Named.Frags.Count - 1, canonicalName);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return newQualifier;
        }

        internal protected IModel TryGetXbimModel
        {
            get {
                lock (this)
                {
                    IModel model = null;
                    if (_model?.TryGetTarget(out model) ?? false)
                        return model;
                    else
                        return null;
                }
            }
        }

        internal protected IModel XbimModel
        {
            get {
                lock (this)
                {
                    IModel model = null;
                    if (_model?.TryGetTarget(out model) ?? false)
                        return model;

                    model = _producer.Invoke();
                    _model = new WeakReference<IModel>(model);
                    return model;
                }
            }
            private set {
                lock (this)
                {
                    if (null != value)
                    {
                        _model = new WeakReference<IModel>(value);
                        _producer = () => value;
                    }
                    else
                    {
                        _model = null;
                        _producer = () => null;
                    }
                }
            }
        }

        internal protected bool IsTemporaryStore
        {
            get => Qualifier.GuidOrNameCase != Qualifier.GuidOrNameOneofCase.Named;
        }

        private static IModel LoadFromFile(IfcModel theModel, ICollection<LogMessage> log)
        {
            var filePathName = theModel.Store.GetFilePathName(false);
            var logger = theModel.Store.Logger;
            logger?.LogInfo("Start loading file '{0}'.", filePathName);
            try
            {
                var model = Xbim.Ifc.IfcStore.Open(filePathName, null, null, theModel.NotifyLoadProgressChanged, Xbim.IO.XbimDBAccess.Read);
                theModel.NotifyOnFinished(ActionType.Load, false, false);
                logger?.LogInfo("File '{0}' has been loaded successfully.", filePathName);
                return model;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Exception while loading '{0}'.", filePathName);
                theModel.NotifyOnFinished(ActionType.Load, false, true);                            
            }
            return null;
        }

        private static WeakReference<IfcStore> RefreshReference(Qualifier q, WeakReference<IfcStore> rs)
        {
            IfcStore s;
            if (rs.TryGetTarget(out s))
            {   // If reference is valid
                if (!s.Qualifier.Equals(q))
                    throw new ArgumentException($"Qualifier of store is different ({s.Qualifier.ToLabel()}");
                return rs;
            }
            else
            {   // If not, recreate store
                return new WeakReference<IfcStore>(new IfcStore(q));
            }
        }

        private static void InitLogging(Logger logInstance)
        {
            XbimLogging.LoggerFactory = logInstance?.LoggerFactory ?? new LoggerFactory();
        }

        #endregion

#pragma warning restore CS1591

        /// <summary>
        /// The logger instance (or null if there is no).
        /// </summary>
        public Logger Logger { get; set; }

        /// <summary>
        /// The source store name.
        /// </summary>
        public string Name
        {
            get {
                switch(Qualifier.GuidOrNameCase)
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
        /// The resource path name of physical store.
        /// </summary>
        public string ResourcePathName
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
        /// The full path file name.
        /// </summary>
        public string GetFilePathName(bool withCanonicalAddons = true, string canonicalSep = "_")
        {
            switch (Qualifier.GuidOrNameCase)
            {
                case Qualifier.GuidOrNameOneofCase.Anonymous:
                    return $"{Path.GetTempPath()}{Path.DirectorySeparatorChar}{Qualifier.Anonymous.ToBase64String()}.ifc";
                case Qualifier.GuidOrNameOneofCase.Named:
                    var fileName = withCanonicalAddons ? Qualifier.Named.ToLabel(canonicalSep, 1, 1) : Name;
                    return $"{Qualifier.Named.Frags[0]}{Path.DirectorySeparatorChar}{fileName}.{Qualifier.Named.Frags.Last()}";
                default:
                    throw new NotSupportedException($"Missing qualifier");
            }
        }

        /// <summary>
        /// The qualifier.
        /// </summary>        
        public Qualifier Qualifier { get; private set; }

        /// <summary>
        /// Get or creates a new model store from file and logger instance.
        /// </summary>
        /// <param name="fileName">File name to load</param>
        /// <param name="logInstance">The logger instance</param>   
        /// <param name="tessellationPrefs">Tessellation preferences</param>
        /// <returns>This instance</returns>
        public static IfcModel GetOrCreateModelStore(string fileName, Logger logInstance, IfcTessellationPrefs tessellationPrefs)
        {
            InitLogging(logInstance);
            var store = new IfcStore(BuildQualifier(fileName));
            var model = new IfcModel(store);
            store.Logger = logInstance;
            store._producer = () => LoadFromFile(model, model.LogMessages);

            tessellationPrefs?.ApplyTo(model);

            return model;
        }

        /// <summary>
        /// A new IFC model from existing internal model.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="logInstance"></param>
        /// <returns></returns>
        public static IfcModel CreateFromModel(IModel model, Logger logInstance)
        {
            var store = new IfcStore(model);
            store.Logger = logInstance;
            return new IfcModel(store);
        }

        /// <summary>
        /// A new IFC model generated by a producer delegate.
        /// </summary>
        /// <param name="producerDelegate"></param>
        /// <param name="ancestor"></param>
        /// <param name="canonicalAddon"></param>
        /// <param name="logInstance"></param>
        /// <returns></returns>
        public static IfcModel CreateFromProducer(ProducerProgressDelegate producerDelegate, 
            Qualifier ancestor, string canonicalAddon, Logger logInstance)
        {
            var store = new IfcStore(BuildQualifier(ancestor, canonicalAddon));
            store.Logger = logInstance;
            var ifcModel = new IfcModel(store);
            store._producer = () => producerDelegate?.Invoke(ifcModel);
            return ifcModel;
        }

        public static IfcModel CreateFromTransform(IfcModel source, 
            TransformerProgressDelegate transformerDelegate, string canoncialName)
        {
            var store = new IfcStore(BuildQualifier(source.Store.Qualifier, canoncialName));
            store.Logger = source.Store.Logger;
            var ifcModel = new IfcModel(store);
            store._producer = () => 
            {
                return transformerDelegate?.Invoke(source.Store.XbimModel, ifcModel);
            };
            return ifcModel;
        }

        /// <summary>
        /// Changes the format extension identifier.
        /// </summary>
        /// <param name="newExtension">Either .ifc, .ifcxml or .ifczip</param>
        public void ChangeFormat(string newExtension)
        {
            if (IsTemporaryStore)
                throw new NotSupportedException("Not allowed for temporary models");

            var ext = Extensions.First(e => e.Equals(newExtension, StringComparison.OrdinalIgnoreCase));
            Qualifier.Named.Frags[Qualifier.Named.Frags.Count - 1] = ext;
        }

        /// <summary>
        /// Renames the current model's physical name. Does not rename the original
        /// physical resource. The change will only have effect when saving this store.
        /// </summary>
        /// <param name="fileNameWithoutExt">The new file name without format extension</param>
        public void Rename(string fileNameWithoutExt)
        {
            if (IsTemporaryStore)
            {
                Logger?.LogInfo($"Renaming temporary store '{Qualifier.Anonymous.ToBase64String()}'");
                Qualifier = BuildQualifier("~", fileNameWithoutExt, "ifc");
            }

            Qualifier.Named.Frags[1] = fileNameWithoutExt;
        }

        /// <summary>
        /// Relocates the file to another folder / directory.
        /// </summary>
        /// <param name="pathName">The absolute path name</param>
        public void Relocate(string pathName)
        {
            if (IsTemporaryStore)
            {
                Logger?.LogInfo($"Relocating temporary store '{Qualifier.Anonymous.ToBase64String()}'");
                Qualifier = BuildQualifier(pathName, Qualifier.Anonymous.ToBase64String(), "ifc");
            }

            Qualifier.Named.Frags[0] = Path.GetDirectoryName($"{pathName}{Path.DirectorySeparatorChar}");
        }

        /// <summary>
        /// Replaces fragments of the file name (without extension) by given fragments
        /// </summary>
        /// <param name="replacePattern">Regular expression identifiying the replacement</param>
        /// <param name="replaceWith">Fragments to insert</param>
        public void RenameWithReplacePattern(string replacePattern, string replaceWith)
        {
            if (IsTemporaryStore)
                throw new NotSupportedException("Not allowed for temporary models");

            var modifiedName = Regex.Replace(
                    Qualifier.Named.Frags[1],
                    replacePattern,
                    replaceWith).Trim();
            Rename(modifiedName);
        }

        /// <summary>
        /// Renames the current model's physical name append the given suffix.
        /// </summary>
        /// <param name="suffix">A suffix</param>
        public void RenameWithSuffix(string suffix)
        {
            Rename($"{Qualifier.Named.Frags[1]}{suffix}{Qualifier.Named.Frags[2]}");
        }

    }
}
