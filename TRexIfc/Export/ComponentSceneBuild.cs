using System;
using System.Collections.Concurrent;

using Bitub.Xbim.Ifc.Export;

using Autodesk.DesignScript.Runtime;

using TRex.Log;
using TRex.Store;
using TRex.Internal;

using Microsoft.Extensions.Logging;

namespace TRex.Export
{
#pragma warning disable CS1591

    /// <summary>
    /// Scene builder utility.
    /// </summary>   
    public class ComponentSceneBuild : ProgressingTask
    {
        #region Internals

        internal ComponentModelExporter Exporter { get; private set; }
        internal IfcModel IfcModel { get; private set; }

        internal ComponentSceneBuild(IfcModel ifcModel, ILoggerFactory loggerFactory)
        {
            Exporter = new ComponentModelExporter(new XbimTesselationContext(loggerFactory), loggerFactory);
            IfcModel = ifcModel;
        }

        protected override LogReason DefaultReason => LogReason.Transformed;

        #endregion

        internal override string Name
        {
            get => $"{IfcModel?.Name}";
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// A new scene exporter instance bound to the given settings and logging.
        /// </summary>
        /// <param name="settings">The settings</param>
        /// <param name="ifcModel">The model about to be exported</param>
        /// <returns>A scene export task</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static ComponentSceneBuild BySettingsAndModel(SceneBuildSettings settings, IfcModel ifcModel)
        {
            if (null == settings)
                throw new ArgumentNullException(nameof(settings));

            var sceneExport = new ComponentSceneBuild(ifcModel, ifcModel.Store.Logger?.LoggerFactory);
            sceneExport.Exporter.Preferences = settings.Preferences;
            return sceneExport;
        }

        /// <summary>
        /// Run the scene export as binary protobuf.
        /// </summary>
        /// <param name="sceneExport">The export</param>
        /// <param name="timeout">The task timeout</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static ComponentScene RunBuildComponentScene(ComponentSceneBuild sceneExport, TimeSpan? timeout = null)
        {
            if (null == sceneExport)
                throw new ArgumentNullException(nameof(sceneExport));

            var model = sceneExport.IfcModel?.XbimModel;
            if (null == model)
                throw new ArgumentNullException(nameof(IfcModel));

            ComponentScene componentScene = null;
            using (var monitor = sceneExport.CreateProgressMonitor(LogReason.Saved))
            {                
                try
                {
                    using (var sceneBuildTask = sceneExport.Exporter.RunExport(model, monitor))
                    {
                        if (timeout.HasValue)
                            sceneBuildTask.Wait((int)timeout.Value.TotalMilliseconds);
                        else
                            sceneBuildTask.Wait();
                        
                        if (!monitor.State.IsCanceled && !monitor.State.IsBroken)
                        {
                            ModelCache.Instance.TryGetOrCreateModel(
                                sceneExport.IfcModel.Qualifier,
                                (q) => new ComponentScene(sceneBuildTask.Result, q, sceneExport.IfcModel.Logger),
                                out componentScene);                            
                            
                            monitor.State.MarkTerminated();
                            sceneExport.IfcModel.OnActionLogged(LogMessage.BySeverityAndMessage(
                                sceneExport.Name, LogSeverity.Info, LogReason.Transformed, "Scene has been built."));
                        }
                        else
                        {
                            monitor.State.MarkTerminated();
                            sceneExport.IfcModel.OnActionLogged(LogMessage.BySeverityAndMessage(
                                sceneExport.Name, LogSeverity.Critical, LogReason.Transformed, "Failed/canceled: Scene build failed."));
                        }
                    }
                }
                catch (Exception e)
                {
                    monitor.State.MarkBroken();
                    sceneExport.IfcModel.OnActionLogged(LogMessage.BySeverityAndMessage(
                        sceneExport.Name, LogSeverity.Error, LogReason.Transformed, "Scene build failed by exception: {0}", e.Message));
                }
                finally
                {
                    monitor.NotifyOnProgressEnd();
                }
            }

            return componentScene;
        }
    }

#pragma warning restore CS1591    
}
