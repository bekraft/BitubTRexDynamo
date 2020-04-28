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
        /// <param name="storeProducer">The store producer</param>
        /// <param name="taskNode">The task node</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static SceneExportSummary[] RunSceneJsonExport(SceneExportSettings settings,
            string filePath,
            IIfcStoreProducer storeProducer, 
            ICancelableTaskNode taskNode)
        {
            List<SceneExportSummary> summaries = new List<SceneExportSummary>();
            foreach (var store in storeProducer)
            {
                
            }
            return summaries.ToArray();
        }

        /// <summary>
        /// Run the scene export as binary protobuf.
        /// </summary>
        /// <param name="settings">The settings (as input)</param>
        /// <param name="filePath">The file path</param>
        /// <param name="storeProducer">The store producer</param>
        /// <param name="taskNode">The task node</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static SceneExportSummary[] RunSceneProtoBExport(SceneExportSettings settings,
            string filePath,
            IIfcStoreProducer storeProducer,
            ICancelableTaskNode taskNode)
        {
            List<SceneExportSummary> summaries = new List<SceneExportSummary>();
            var exporter = new IfcSceneExporter(new XbimTesselationContext());

            foreach (var store in storeProducer)
            {
                string sceneFileName = $@"{filePath}\{Path.GetFileNameWithoutExtension(store.FileName)}.scene";
                try
                {
                    var sceneTask = exporter.Run(store.XbimModel);
                    sceneTask.Wait();
                    
                    using (var binStream = File.Create(sceneFileName))
                    {
                        var binScene = sceneTask.Result.Scene.ToByteArray();
                        binStream.Write(binScene, 0, binScene.Length);
                    }

                    summaries.Add(SceneExportSummary.ByResults(
                        sceneFileName,
                        LogMessage.BySeverityAndMessage(Severity.Info, "Wrote binary scene file '{0}'", sceneTask.Result.Scene.Name)));
                }
                catch (Exception e)
                {
                    summaries.Add(SceneExportSummary.ByResults(
                        sceneFileName,
                        LogMessage.BySeverityAndMessage(Severity.Critical, "Caught exception '{0}': {1}", e.Message, e)));                    
                }
            }
            return summaries.ToArray();
        }

        /// <summary>
        /// Run the scene export according to selected format option.
        /// </summary>
        /// <param name="settings">The settings (as input)</param>
        /// <param name="filePath">The file path</param>
        /// <param name="storeProducer">The store producer</param>
        /// <param name="extension">The extension (either ".json" or ".scene")</param>
        /// <param name="taskNode">The task node</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static SceneExportSummary[] RunSceneExport(SceneExportSettings settings, 
            string filePath, 
            IIfcStoreProducer storeProducer,
            string extension,
            ICancelableTaskNode taskNode)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".json":
                    return RunSceneJsonExport(settings, filePath, storeProducer, taskNode);
                case ".scene":
                    return RunSceneProtoBExport(settings, filePath, storeProducer, taskNode);

                default:
                    throw new NotImplementedException($"Missing format option '{extension}");
            }
        }

        [IsVisibleInDynamoLibrary(false)]
        public static SceneExportSummary[] RunSceneExport(object settings,
            object filePath,
            object storeProducer,
            string extension,
            object taskNode)
        {
            return RunSceneExport(settings as SceneExportSettings,
                filePath as string,
                storeProducer as IIfcStoreProducer,
                extension as string,
                taskNode as ICancelableTaskNode);
        }
    }

#pragma warning restore CS1591
}
