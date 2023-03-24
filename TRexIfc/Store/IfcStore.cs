using System;
using System.IO;
using System.Collections.Generic;

using Bitub.Dto;

using Xbim.Common;
using Xbim.Ifc;

using Autodesk.DesignScript.Runtime;
using Microsoft.Extensions.Logging;

using TRex.Log;
using Xbim.Common.Configuration;

namespace TRex.Store
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

        // The model producer hook 
        internal Func<IModel> Producer { get; private set; }

        #region Private internals

        private WeakReference<IModel> reference;
        private readonly object monitor = new object();        

        static IfcStore()
        {
            XbimServices.Current.ConfigureServices(s => s.AddXbimToolkit(opt => opt.AddHeuristicModel()));
        }

        private IfcStore(Logger logger)
        {
            Logger = logger;
        }

        private IfcStore(IModel model, Logger logger)
        {
            XbimModel = model;
            Logger = logger;
        }

        private static IModel LoadFromFile(IfcModel theModel, IfcTessellationPrefs prefs, string filePathName)
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

        internal protected IModel TryGetXbimModel
        {
            get {
                lock (this)
                {
                    IModel model = null;
                    if (reference?.TryGetTarget(out model) ?? false)
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
                    if (reference?.TryGetTarget(out model) ?? false)
                        return model;
                    
                    model = Producer?.Invoke();
                    reference = new WeakReference<IModel>(model);

                    return model;
                }
            }
            private set {
                lock (monitor)
                {
                    if (null != value)
                    {
                        reference = new WeakReference<IModel>(value);
                        Producer = () => value;
                    }
                    else
                    {
                        reference = null;
                        Producer = () => null;
                    }
                }
            }
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
        /// <param name="logger">The logger instance</param>   
        /// <param name="tessellationPrefs">Tessellation preferences</param>
        /// <returns>This instance</returns>
        public static IfcModel ByIfcModelFile(string fileName, Logger logger, IfcTessellationPrefs tessellationPrefs)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            InitLogging(logger);

            var qualifier = ProgressingModelTask<IfcModel>.BuildQualifierByFilePathName(fileName);
            IfcModel ifcModel;
            if (!ModelCache.Instance.TryGetOrCreateModel(qualifier, q => new IfcModel(new IfcStore(logger), qualifier), out ifcModel))
            {
                ifcModel.Store.Producer = () => LoadFromFile(ifcModel, tessellationPrefs, fileName);
                tessellationPrefs?.ApplyToModel(ifcModel);
            }

            return ifcModel;
        }

        /// <summary>
        /// Cancels all tasks in progress.
        /// </summary>
        /// <param name="ifcModel">The model</param>
        /// <returns>Model with cancelled task mark</returns>
        public static IfcModel MarkAsCancelled(IfcModel ifcModel)
        {
            ifcModel.CancelAll();
            return ifcModel;
        }

        /// <summary>
        /// A new IFC model from existing internal model.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="logger">The log instance</param>
        /// <param name="qualifier">The qualifier</param>
        /// <returns></returns>
        public static IfcModel ByXbimModel(IModel model, Qualifier qualifier, Logger logger)
        {
            IfcModel ifcModel;
            ModelCache.Instance.TryGetOrCreateModel(qualifier, q => new IfcModel(new IfcStore(model, logger), qualifier), out ifcModel);
            return ifcModel;
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
            IfcModel ifcModel;

            if (string.IsNullOrWhiteSpace(canoncialName))
                canoncialName = DateTime.Now.Ticks.ToString();

            var qualifier = ProgressingModelTask<IfcModel>.BuildCanonicalQualifier(source.Qualifier, canoncialName);
            if (!ModelCache.Instance.TryGetOrCreateModel(qualifier, q => new IfcModel(new IfcStore(source.Store.Logger), qualifier), out ifcModel))
            {
                ifcModel.Store.Producer = () =>
                {
                    if (source.IsCanceled)
                    {
                        ifcModel.CancelAll();
                        ifcModel.Logger.LogWarning("Transform of '{0}' to '{1}' has been canceled.", source.CanonicalName(), ifcModel.CanonicalName());
                        return null;
                    }
                    else
                    {
                        return transform?.Invoke(source.XbimModel, ifcModel);
                    }
                };
            }            
            return ifcModel;
        }
    }
}
