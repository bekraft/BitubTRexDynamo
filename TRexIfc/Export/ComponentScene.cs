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
    /// <summary>
    /// Exported component scene model.
    /// </summary>
    public class ComponentScene : ProgressingTask
    {
        /// <summary>
        /// Allowed extensions for <see cref="SaveAs(ComponentScene, string, string)"/>.
        /// </summary>
        public readonly static string[] saveAsExtensions = new string[] { "json", "scene" };

        /// <summary>
        /// Allowed extensions for <see cref="ExportAs(ComponentScene, string, string)"/>.
        /// </summary>
        public readonly static string[] exportAsExtensions;

        #region Internals        

        internal Bitub.Dto.Scene.ComponentScene SceneModel { get; private set; }

        internal ComponentScene(Bitub.Dto.Scene.ComponentScene componentScene)
        {
            SceneModel = componentScene;
        }

        static ComponentScene() 
        {
            exportAsExtensions = new TRexAssimp.TRexAssimpExport().Extensions;
        }

        internal static string TestAndCorrectExtension(string filePathName, string extension)
        {
            var existing = Path.GetExtension(filePathName)?.ToLower().Substring(1);
            if (string.IsNullOrWhiteSpace(existing))
                return filePathName;

            if (existing.Equals(extension.ToLower()))
            {
                return filePathName;
            }
            else
            {
                return Path.ChangeExtension(filePathName, extension);
            }
        }

        #endregion

        /// <summary>
        /// Saves the current component model as it is to JSON or binary format.
        /// </summary>
        /// <param name="scene">The scene to save</param>
        /// <param name="filePathName">The filename</param>
        /// <param name="extension">The desired extension to use</param>
        /// <returns></returns>
        public static ComponentScene SaveAs(ComponentScene scene, string filePathName, string extension)
        {
            var fileName = TestAndCorrectExtension(filePathName, extension);
            switch (extension.ToLower())
            {
                case "scene":
                    using (var binStream = File.Create(fileName))
                    {
                        var binScene = scene.SceneModel.ToByteArray();
                        binStream.Write(binScene, 0, binScene.Length);
                    }
                    break;
                case "json":
                    using (var textStream = File.CreateText(fileName))
                    {
                        var json = new JsonFormatter(new JsonFormatter.Settings(false)).Format(scene.SceneModel);
                        textStream.Write(json);
                    }
                    break;
                default:
                    throw new NotImplementedException($"Missing implementation for '{extension}'");
            }

            scene.ActionLog.Add(
                LogMessage.BySeverityAndMessage(
                    scene.SceneModel.Metadata.Name, LogSeverity.Info, LogReason.Saved, "Scene save to {0}.", fileName));

            return scene;
        }

        public static ComponentScene ExportAs(ComponentScene scene, string filePathName, string extension)
        {
            var fileName = TestAndCorrectExtension(filePathName, extension);
            var aiExporter = new TRexAssimp.TRexAssimpExport();

            if (ai)
            return scene;
        }
    }
}
