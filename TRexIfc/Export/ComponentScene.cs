using System;
using System.IO;
using System.Linq;

using Bitub.Dto;

using Autodesk.DesignScript.Runtime;
using Google.Protobuf;

using Log;
using Store;

namespace Export
{
    /// <summary>
    /// Exported component scene model.
    /// </summary>
    public class ComponentScene : ProgressingModelTask<ComponentScene>
    {
        /// <summary>
        /// Allowed extensions for <see cref="Save(ComponentScene, string, string)"/>.
        /// </summary>
        public readonly static string[] saveAsExtensions = new string[] { "json", "scene" };

        /// <summary>
        /// Allowed extensions for <see cref="Export(ComponentScene, string, string)"/>.
        /// </summary>
        public readonly static string[] exportAsExtensions;

#pragma warning disable CS1591

        #region Internals        

        internal Bitub.Dto.Scene.ComponentScene SceneModel { get; private set; }

        internal ComponentScene(Bitub.Dto.Scene.ComponentScene componentScene, Logger logger) : base(logger)
        {
            SceneModel = componentScene;
            Qualifier = System.Guid.NewGuid().ToQualifier();
        }

        static ComponentScene() 
        {
            exportAsExtensions = new TRexAssimp.TRexAssimpExport().Extensions;
        }

        protected override ComponentScene RequalifiyModel(Qualifier qualifier)
        {
            return new ComponentScene(SceneModel, Logger)
            {
                Qualifier = qualifier
            };
        }

        protected override LogReason DefaultReason => LogReason.Saved;

        #endregion

#pragma warning restore CS1591

        /// <summary>
        /// Saves the current component model as it is to JSON or binary format.
        /// </summary>
        /// <param name="scene">The scene to save</param>
        /// <param name="extension">The desired extension to use</param>
        /// <param name="canonicalSeparator">Canonical fragment seperator.</param>
        /// <returns>The saved scene</returns>
        public static ComponentScene Save(ComponentScene scene, string extension, string canonicalSeparator)
        {
            if (null == scene)
                throw new ArgumentNullException(nameof(scene));

            var qualifier = BuildQualifierByExtension(scene.Qualifier, extension);
            var fileName = GetFilePathName(qualifier, canonicalSeparator, true);

            using (var monitor = scene.CreateProgressMonitor(LogReason.Saved))
            {
                try
                {
                    monitor.NotifyProgressEstimateUpdate(1);
                    monitor.NotifyOnProgressChange(0, "Start saving");
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
                            var msg = $"Unknown extension format '{extension}'.";
                            scene.ActionLog.Add(LogMessage.ByErrorMessage(scene.Name, LogReason.Saved, msg));
                            monitor.State.MarkBroken();

                            throw new NotImplementedException(msg);
                    }

                    scene.ActionLog.Add(
                        LogMessage.BySeverityAndMessage(scene.Name, LogSeverity.Info, LogReason.Saved, "Scene save to '{0}'.", fileName));
                    monitor.NotifyOnProgressChange(1, "Saved file");
                }
                catch (Exception e)
                {
                    monitor.State.MarkBroken();

                    scene.Logger?.LogError(e, "An exception has been caught: {0}", e.Message);
                    scene.ActionLog.Add(
                        LogMessage.ByErrorMessage(scene.Name, LogReason.Saved, "Exception '{0}' thrown while saving to '{1}'.", e.Message, fileName));
                }

                monitor.State.MarkTerminated();
                monitor.NotifyOnProgressEnd();
            }
            return scene;
        }

        /// <summary>
        /// Exports the current component scene to the given format indicated by the extension.
        /// </summary>
        /// <param name="scene">The scene</param>
        /// <param name="extension">The format extension (one of <see cref="exportAsExtensions"/></param>
        /// <param name="canonicalSeparator">The canonical fragment separator</param>
        /// <returns>The exported scene</returns>
        public static ComponentScene Export(ComponentScene scene, string extension, string canonicalSeparator)
        {
            if (null == scene)
                throw new ArgumentNullException(nameof(scene));

            var qualifier = BuildQualifierByExtension(scene.Qualifier, extension);
            var fileName = GetFilePathName(qualifier, canonicalSeparator, true);

            using (var monitor = scene.CreateProgressMonitor(LogReason.Saved))
            {
                try
                {
                    var exp = new TRexAssimp.TRexAssimpExport();
                    monitor.NotifyProgressEstimateUpdate(1);
                    monitor.NotifyOnProgressChange(0, "Start exporting");

                    if (!exp.ExportTo(scene.SceneModel, fileName, extension))
                    {
                        monitor.NotifyOnProgressChange(1, "Exported");
                        scene.ActionLog.Add(
                            LogMessage.ByErrorMessage(scene.Name, LogReason.Saved, "An error occured while exporting to '{0}'. {1}", fileName, exp.StatusMessage));
                    }
                    else
                    {
                        monitor.State.MarkBroken();

                        scene.ActionLog.Add(
                            LogMessage.BySeverityAndMessage(scene.Name, LogSeverity.Info, LogReason.Saved, "Scene exported to '{0}'.", fileName));
                    }
                }
                catch (Exception e)
                {
                    monitor.State.MarkBroken();

                    scene.Logger?.LogError(e, "An exception has been caught: {0}", e.Message);
                    scene.ActionLog.Add(
                        LogMessage.ByErrorMessage(scene.Name, LogReason.Saved, "Exception '{0}' thrown while exporting to '{1}'.", e.Message, fileName));
                }

                monitor.State.MarkTerminated();
                monitor.NotifyOnProgressEnd();                
            }

            return scene;
        }
    }
}
