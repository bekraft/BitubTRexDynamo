using System;
using System.IO;
using System.Linq;

using Bitub.Ifc.Export;

using Autodesk.DesignScript.Runtime;
using Google.Protobuf;

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

        internal readonly ComponentModelExporter exporter;
        internal readonly IfcModel ifcModel;

        internal SceneExport(IfcModel ifcModel, ILoggerFactory loggerFactory)
        {
            this.exporter = new ComponentModelExporter(new XbimTesselationContext(loggerFactory), loggerFactory);
            this.ifcModel = ifcModel;
        }

        protected override LogReason DefaultReason => LogReason.Saved;

        #endregion

        internal override string Name
        {
            get => $"{ifcModel?.Name}";
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
            sceneExport.exporter.Preferences = settings.Preferences;
            return sceneExport;
        }

        /// <summary>
        /// Run the scene export as binary protobuf.
        /// </summary>
        /// <param name="sceneExport">The export</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static ComponentScene BuildScene(SceneExport sceneExport)
        {
            if (null == sceneExport)
                throw new ArgumentNullException(nameof(sceneExport));

            string fileName = sceneExport.ifcModel.CanonicalName(canonicalSeparator);
            string sceneFileName = $@"{filePath}{Path.DirectorySeparatorChar}{fileName}.{sceneExport.Extension}";

            if (sceneExport.ifcModel.IsCanceled)
            {
                sceneExport.ifcModel.ActionLog.Add(LogMessage.BySeverityAndMessage(
                    sceneExport.ifcModel.FileName, LogSeverity.Info, LogReason.Saved, "Export canceled of '{0}'.", sceneFileName));
                return sceneExport.ifcModel;
            }

            using (var monitor = sceneExport.CreateProgressMonitor(LogReason.Saved))
            {
                try
                {
                    var internalModel = sceneExport.ifcModel?.XbimModel;
                    if (null == internalModel)
                        throw new ArgumentNullException(nameof(ifcModel));

                    using (var sceneTask = sceneExport.exporter.RunExport(internalModel, monitor))
                    {
                        sceneTask.Wait();
                        if (!monitor.State.IsCanceled && !monitor.State.IsBroken)
                        {
                            monitor.State.MarkTerminated();
                            sceneExport.ifcModel.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                sceneExport.ifcModel.FileName, LogSeverity.Info, LogReason.Saved, "Exported as file '{0}'", sceneFileName));
                        }
                        else
                        {
                            monitor.State.MarkTerminated();
                            sceneExport.ifcModel.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                sceneExport.ifcModel.FileName, LogSeverity.Critical, LogReason.Saved, "Failed/canceled: Exporting as file '{0}'", sceneFileName));
                        }
                    }
                }
                catch (Exception e)
                {
                    monitor.State.MarkBroken();
                    sceneExport.ifcModel.ActionLog.Add(LogMessage.BySeverityAndMessage(
                        sceneExport.ifcModel.FileName, LogSeverity.Critical, LogReason.Saved, "Export failure ({0}) '{1}'", e.Message, sceneFileName));
                }
                finally
                {
                    monitor.NotifyOnProgressEnd();
                }
            }

            return sceneExport.ifcModel;
        }
    }

#pragma warning restore CS1591    
}
