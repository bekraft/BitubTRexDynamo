using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Linq;

using Bitub.Dto;

using Xbim.Common;
using Xbim.Ifc;

using Autodesk.DesignScript.Runtime;

using System.Collections.Generic;

using Log;
using Internal;
using System.Data;

namespace Store
{
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
        private WeakReference<IModel> modelRef;
        private readonly object monitor = new object();

        // The model producer hook 
        internal Func<IModel> Producer { get; private set; }

        static IfcStore()
        {
            Xbim.Ifc.IfcStore.ModelProviderFactory = new DefaultModelProviderFactory();
            Xbim.Ifc.IfcStore.ModelProviderFactory.UseHeuristicModelProvider();
        }

        private IfcStore(Logger logger)
        {
            Logger = logger;
        }

        private IfcStore(IModel model)
        {
            XbimModel = model;
        }

        private IfcStore(Func<IModel> producerDelegate)
        {
            Producer = producerDelegate;
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
                    if (modelRef?.TryGetTarget(out model) ?? false)
                        return model;
                    else
                        return null;
                }
            }
        }

        internal protected IModel XbimModel
        {
            get {
                lock (monitor)
                {
                    IModel model = null;
                    if (modelRef?.TryGetTarget(out model) ?? false)
                        return model;
                    
                    model = Producer?.Invoke();
                    modelRef = new WeakReference<IModel>(model);

                    return model;
                }
            }
            private set {
                lock (monitor)
                {
                    if (null != value)
                    {
                        modelRef = new WeakReference<IModel>(value);
                        Producer = () => value;
                    }
                    else
                    {
                        modelRef = null;
                        Producer = () => null;
                    }
                }
            }
        }

        private static IModel LoadFromFile(IfcModel theModel, IfcTessellationPrefs prefs, string filePathName, ICollection<LogMessage> log)
        {
            var logger = theModel.Store.Logger;
            logger?.LogInfo("Start loading file '{0}'.", filePathName);
            try
            {                
                var model = Xbim.Ifc.IfcStore.Open(filePathName, null, null, theModel.NotifyLoadProgressChanged, Xbim.IO.XbimDBAccess.Read);
                prefs?.ApplyTo(model);
                theModel.NotifyOnProgressEnded(LogReason.Loaded, false, false);
                logger?.LogInfo("File '{0}' has been loaded successfully.", filePathName);
                theModel.Store.TimeStamp = File.GetCreationTime(filePathName);

                return model;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Exception while loading '{0}'.", filePathName);
                theModel.NotifyOnProgressEnded(LogReason.Loaded, false, true);                            
            }
            return null;
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
        /// Gets the time stamp.
        /// </summary>
        public DateTime TimeStamp { get; private set; } = DateTime.Now;

        /// <summary>
        /// Get or creates a new model store from file and logger instance.
        /// </summary>
        /// <param name="fileName">File name to load</param>
        /// <param name="logInstance">The logger instance</param>   
        /// <param name="tessellationPrefs">Tessellation preferences</param>
        /// <returns>This instance</returns>
        public static IfcModel ByIfcModelFile(string fileName, Logger logInstance, IfcTessellationPrefs tessellationPrefs, bool createNope = false)
        {
            InitLogging(logInstance);
            var store = new IfcStore(logInstance);
            var model = new IfcModel(store, BuildQualifier(fileName));
            store.Producer = () => LoadFromFile(model, tessellationPrefs, fileName, model.ActionLog);

            if (createNope)
            {
                model.CancelAll();
                store.Logger.LogWarning("Loading '{0}' has been canceled.", fileName);
            }

            tessellationPrefs?.ApplyToModel(model);

            return model;
        }

        /// <summary>
        /// A new IFC model from existing internal model.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="logInstance">The log instance</param>
        /// <param name="qualifier">The qualifier</param>
        /// <returns></returns>
        public static IfcModel ByXbimModel(IModel model, Qualifier qualifier, Logger logInstance)
        {
            var store = new IfcStore(model);
            store.Logger = logInstance;
            return new IfcModel(store, qualifier);
        }

        /// <summary>
        /// A new IFC model generated by transformation delegate.
        /// </summary>
        /// <param name="source">The source model</param>
        /// <param name="transform">A transform expecting an <see cref="IModel"/> and a <see cref="IfcModel"/> to associate the result to.</param>
        /// <param name="canoncialName">The canonical fragment</param>
        /// <returns>A new <see cref="IfcModel"/> with transform delegate</returns>
        internal static IfcModel ByTransform(IfcModel source, Func<IModel, IfcModel, IModel> transform, string canoncialName)
        {
            var store = new IfcStore(source.Store.Logger);            
            var ifcModel = new IfcModel(store, BuildQualifier(source.Qualifier, canoncialName));
            store.Producer = () =>
            {
                if (source.IsCanceled)
                {
                    ifcModel.CancelAll();
                    store.Logger.LogWarning("Transform of '{0}' to '{1}' has been canceled.", source.CanonicalName(), ifcModel.CanonicalName());
                    return null;
                }
                else
                {
                    return transform?.Invoke(source.XbimModel, ifcModel);
                }
            };
            
            return ifcModel;
        }
    }
}
