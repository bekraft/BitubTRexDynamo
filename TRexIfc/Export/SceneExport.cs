using System;
using System.Collections.Concurrent;

using Bitub.Ifc.Export;

using Autodesk.DesignScript.Runtime;

using Log;
using Store;
using Internal;

using Microsoft.Extensions.Logging;

namespace Export
{
#pragma warning disable CS1591

    /// <summary>
    /// Scene export utility.
    /// </summary>   
    public class SceneExport : ProgressingTask
    {
        #region Internals

        internal ComponentModelExporter Exporter { get; private set; }
        internal IfcModel IfcModel { get; private set; }

        internal SceneExport(IfcModel ifcModel, ILoggerFactory loggerFactory)
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
        public static SceneExport BySettingsAndModel(SceneExportSettings settings, IfcModel ifcModel)
        {
            if (null == settings)
                throw new ArgumentNullException(nameof(settings));

            var sceneExport = new SceneExport(ifcModel, ifcModel.Store.Logger?.LoggerFactory);
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
        public static ComponentScene RunBuildComponentScene(SceneExport sceneExport, TimeSpan? timeout = null)
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
                            monitor.State.MarkTerminated();
                            sceneExport.IfcModel.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                sceneExport.Name, LogSeverity.Info, LogReason.Transformed, "Scene has been built."));
                        }
                        else
                        {
                            componentScene = new ComponentScene(sceneBuildTask.Result, sceneExport.IfcModel.Logger);
                            componentScene.Qualifier = sceneExport.IfcModel.Qualifier;

                            monitor.State.MarkTerminated();
                            sceneExport.IfcModel.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                sceneExport.Name, LogSeverity.Critical, LogReason.Transformed, "Failed/canceled: Scene build failed."));
                        }
                    }
                }
                catch (Exception e)
                {
                    monitor.State.MarkBroken();
                    sceneExport.IfcModel.ActionLog.Add(LogMessage.BySeverityAndMessage(
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
