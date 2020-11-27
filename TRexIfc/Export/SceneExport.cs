using System;
using System.IO;
using System.Linq;

using Bitub.Ifc.Scene;

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
        public static string[] Extensions = new string[] { "json", "scene" };

        #region Internals

        internal readonly IfcSceneExporter Exporter;
        internal readonly IfcModel IfcModel;
        internal string Extension { get; set; }

        internal SceneExport(IfcModel ifcModel, ILoggerFactory loggerFactory)
        {
            Exporter = new IfcSceneExporter(new XbimTesselationContext(loggerFactory), loggerFactory);
            IfcModel = ifcModel;
        }

        protected override LogReason DefaultReason => LogReason.Saved;

        #endregion

        internal override string Name
        {
            get => $"{IfcModel?.Name}.{Extension}";
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
            sceneExport.Exporter.Settings = settings.Settings;
            return sceneExport;
        }

        /// <summary>
        /// Run the scene export as binary protobuf.
        /// </summary>
        /// <param name="sceneExport">The export</param>
        /// <param name="filePath">The file path</param>
        /// <param name="canonicalSeparator">If set, canonical names will be used</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IfcModel RunSceneExport(SceneExport sceneExport, string filePath, string canonicalSeparator = null)
        {
            if (null == sceneExport)
                throw new ArgumentNullException(nameof(sceneExport));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            string fileName = sceneExport.IfcModel.CanonicalName(canonicalSeparator);
            string sceneFileName = $@"{filePath}{Path.DirectorySeparatorChar}{fileName}.{sceneExport.Extension}";

            if (sceneExport.IfcModel.IsCanceled)
            {
                sceneExport.IfcModel.ActionLog.Add(LogMessage.BySeverityAndMessage(
                    sceneExport.IfcModel.FileName, LogSeverity.Info, LogReason.Saved, "Export canceled of '{0}'.", sceneFileName));
                return sceneExport.IfcModel;
            }

            using (var monitor = sceneExport.CreateProgressMonitor(LogReason.Saved))
            {
                try
                {
                    var internalModel = sceneExport.IfcModel?.XbimModel;
                    if (null == internalModel)
                        throw new ArgumentNullException(nameof(IfcModel));

                    using (var sceneTask = sceneExport.Exporter.Run(internalModel, monitor))
                    {
                        sceneTask.Wait();
                        if (!monitor.State.IsCanceled && !monitor.State.IsBroken)
                        {
                            switch (sceneExport.Extension.ToLower())
                            {
                                case "scene":
                                    using (var binStream = File.Create(sceneFileName))
                                    {
                                        var binScene = sceneTask.Result.Scene.ToByteArray();
                                        binStream.Write(binScene, 0, binScene.Length);
                                    }
                                    break;
                                case "json":
                                    using (var textStream = File.CreateText(sceneFileName))
                                    {
                                        var json = new JsonFormatter(new JsonFormatter.Settings(false)).Format(sceneTask.Result.Scene);
                                        textStream.Write(json);
                                    }
                                    break;
                                default:
                                    throw new NotImplementedException($"Missing implementation for '{sceneExport.Extension}'");
                            }
                            monitor.State.MarkTerminated();
                            sceneExport.IfcModel.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                sceneExport.IfcModel.FileName, LogSeverity.Info, LogReason.Saved, "Exported as file '{0}'", sceneFileName));
                        }
                        else
                        {
                            monitor.State.MarkTerminated();
                            sceneExport.IfcModel.ActionLog.Add(LogMessage.BySeverityAndMessage(
                                sceneExport.IfcModel.FileName, LogSeverity.Critical, LogReason.Saved, "Failed/canceled: Exporting as file '{0}'", sceneFileName));
                        }
                    }
                }
                catch (Exception e)
                {
                    monitor.State.MarkBroken();
                    sceneExport.IfcModel.ActionLog.Add(LogMessage.BySeverityAndMessage(
                        sceneExport.IfcModel.FileName, LogSeverity.Critical, LogReason.Saved, "Export failure ({0}) '{1}'", e.Message, sceneFileName));
                }
                finally
                {
                    monitor.NotifyOnProgressEnd();
                }
            }

            return sceneExport.IfcModel;
        }

        /// <summary>
        /// Sets the extension of scene model
        /// </summary>
        /// <param name="extension">Either "json" or "sceen".</param>
        /// <param name="sceneExport">The scene export</param>
        /// <returns>The scene export</returns>
        public static SceneExport ExportAs(SceneExport sceneExport, string extension)
        {
            if (null == sceneExport)
                throw new ArgumentException(nameof(sceneExport));
            if (!Extensions.Contains(extension.ToLower()))
                throw new ArgumentException($"Expecting one of: {string.Join(", ", Extensions)}");
            sceneExport.Extension = extension.ToLower();
            return sceneExport;
        }
    }

#pragma warning restore CS1591    
}
