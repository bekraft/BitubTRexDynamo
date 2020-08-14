using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bitub.Ifc.Scene;

using Autodesk.DesignScript.Runtime;
using Google.Protobuf;

using Log;
using Store;
using Task;
using Internal;
using Bitub.Transfer.Scene;
using Bitub.Transfer;
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

        internal SceneExport(ILoggerFactory loggerFactory)
        {            
            Exporter = new IfcSceneExporter(new XbimTesselationContext(loggerFactory), loggerFactory);
        }

        protected override LogReason DefaultReason => LogReason.Saved;

        #endregion

        [IsVisibleInDynamoLibrary(false)]
        internal override string Name
        {
            get => "Scene Export";
        }

        /// <summary>
        /// A new scene exporter instance bound to the given settings and logging.
        /// </summary>
        /// <param name="settings">The settings</param>
        /// <param name="logger">The logging</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static SceneExport CreateSceneExport(SceneExportSettings settings, Logger logger)
        {
            if (null == settings)
                throw new ArgumentNullException("settings");

            var sceneExport = new SceneExport(logger?.LoggerFactory);              
            sceneExport.Exporter.Settings = settings.Settings;
            return sceneExport;
        }

        /// <summary>
        /// Run the scene export as binary protobuf.
        /// </summary>
        /// <param name="sceneExport">The export</param>
        /// <param name="filePath">The file path</param>
        /// <param name="ifcModel">The model producer</param>
        /// <param name="extension">The format extension</param>
        /// <param name="canonicalSeparator">If set, canonical names will be used</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static LogMessage RunSceneExport(SceneExport sceneExport,            
            IfcModel ifcModel,
            string filePath,
            string extension,
            string canonicalSeparator = null)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException("filePath");
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentNullException("extension");

            string fileName = string.IsNullOrEmpty(canonicalSeparator) ? ifcModel.Name : ifcModel.CanonicalName(canonicalSeparator);
            string sceneFileName = $@"{filePath}{Path.DirectorySeparatorChar}{fileName}.{extension}";
            try
            {
                var internalModel = ifcModel?.Store.XbimModel;
                if (null == internalModel)
                    throw new ArgumentNullException("ifcModel");

                using (var sceneTask = sceneExport.Exporter.Run(internalModel, sceneExport.CreateProgressMonitor(LogReason.Saved)))
                {
                    // TODO Time out
                    sceneTask.Wait();

                    switch (extension.ToLower())
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
                                var json = new JsonFormatter(new JsonFormatter.Settings(true)).Format(sceneTask.Result.Scene);
                                textStream.Write(json);
                            }
                            break;
                        default:
                            throw new NotImplementedException($"Missing implementation for '{extension}'");
                    }

                    return LogMessage.BySeverityAndMessage(
                        ifcModel.FileName, LogSeverity.Info, LogReason.Saved, "Wrote {0} ({1}) scene file '{2}'", extension, sceneTask.Result.Scene.Name, sceneFileName);
                }
            }
            catch (Exception e)
            {
                return LogMessage.BySeverityAndMessage(
                    ifcModel.FileName, LogSeverity.Critical, LogReason.Saved, "({0}) '{1}'", e.Message, sceneFileName);
            }
        }

#pragma warning restore CS1591

        /// <summary>
        /// Returns the current exporter settings.
        /// </summary>
        public SceneExportSettings Settings
        {
            get => new SceneExportSettings(Exporter.Settings);
        }

        /// <summary>
        /// Reads settings from persitent configuration file.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="logger">The logger</param>
        /// <returns>An export setting</returns>
        public static SceneExport BySavedSettings(string fileName, Logger logger)
        {
            return SceneExport.CreateSceneExport(
                new SceneExportSettings(IfcSceneExportSettings.ReadFrom(fileName)), logger);
        }
    }
}
