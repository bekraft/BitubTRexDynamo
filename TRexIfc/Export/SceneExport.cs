using Autodesk.DesignScript.Runtime;
using Bitub.Ifc.Scene;
using Google.Protobuf;
using Log;
using Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task;

namespace Export
{
#pragma warning disable CS1591

    [IsVisibleInDynamoLibrary(false)]
    public class SceneExport
    {
        #region Internals

        internal SceneExport()
        {
        }

        #endregion

        /// <summary>
        /// Run the scene export as JSON.
        /// </summary>
        /// <param name="settings">The settings (as input)</param>
        /// <param name="filePath">The file path</param>
        /// <param name="ifcModel">The model producer</param>
        /// <param name="taskNode">The task node</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static SceneExportSummary RunSceneJsonExport(SceneExportSettings settings,
            string filePath,
            IfcModel ifcModel, 
            ICancelableTaskNode taskNode)
        {
            return null;
        }

        /// <summary>
        /// Run the scene export as binary protobuf.
        /// </summary>
        /// <param name="settings">The settings (as input)</param>
        /// <param name="filePath">The file path</param>
        /// <param name="ifcModel">The model producer</param>
        /// <param name="taskNode">The task node</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static SceneExportSummary RunSceneProtoBExport(SceneExportSettings settings,
            string filePath,
            IfcModel ifcModel,
            ICancelableTaskNode taskNode)
        {
            List<SceneExportSummary> summaries = new List<SceneExportSummary>();
            var exporter = new IfcSceneExporter(new XbimTesselationContext());
            exporter.OnProgressChange += (e) => taskNode.Report(e.Percentage, e.StateObject);

            string sceneFileName = $@"{filePath}{Path.DirectorySeparatorChar}{ifcModel.Name}.scene";
            try
            {
                var sceneTask = exporter.Run(ifcModel.Store.XbimModel);
                sceneTask.Wait();
                    
                using (var binStream = File.Create(sceneFileName))
                {
                    var binScene = sceneTask.Result.Scene.ToByteArray();
                    binStream.Write(binScene, 0, binScene.Length);
                }

                return SceneExportSummary.ByResults(
                    sceneFileName,
                    LogMessage.BySeverityAndMessage(Severity.Info, ActionType.Save, "Wrote binary scene file '{0}'", sceneTask.Result.Scene.Name));
            }
            catch (Exception e)
            {
                return SceneExportSummary.ByResults(
                    sceneFileName,
                    LogMessage.BySeverityAndMessage(Severity.Critical, ActionType.Save, "Caught exception '{0}': {1}", e.Message, e));
            }
        }

        /// <summary>
        /// Run the scene export according to selected format option.
        /// </summary>
        /// <param name="settings">The settings (as input)</param>
        /// <param name="filePath">The file path</param>
        /// <param name="ifcModel">The model producer</param>
        /// <param name="extension">The extension (either ".json" or ".scene")</param>
        /// <param name="taskNode">The task node</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static SceneExportSummary RunSceneExport(SceneExportSettings settings,
            string filePath,
            IfcModel ifcModel,
            string extension,
            ICancelableTaskNode taskNode)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".json":
                    return RunSceneJsonExport(settings, filePath, ifcModel, taskNode);
                case ".scene":
                    return RunSceneProtoBExport(settings, filePath, ifcModel, taskNode);

                default:
                    throw new NotImplementedException($"Missing format option '{extension}");
            }
        }
    }

#pragma warning restore CS1591
}
