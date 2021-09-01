using System;
using System.IO;
using System.Linq;

using Bitub.Dto;

using Autodesk.DesignScript.Runtime;
using Google.Protobuf;

using TRex.Log;
using TRex.Store;
using TRex.Geom;

namespace TRex.Export
{
    /// <summary>
    /// Exported component scene model.
    /// </summary>
    public class ComponentScene : ProgressingModelTask<ComponentScene>
    {
        /// <summary>
        /// Allowed extensions for <see cref="Save(ComponentScene, string, string)"/>.
        /// </summary>
        public readonly static Format[] saveAsFormats = new Format[]
        {
            new Format("json", "json", "JSON Text"),
            new Format("scene", "scene", "Binary File")
        };

        /// <summary>
        /// Allowed extensions for <see cref="Export(ComponentScene, UnitScale, CRSTransform, string, string)"/>.
        /// </summary>
        public readonly static Format[] exportAsFormats;

#pragma warning disable CS1591

        #region Internals        

        internal Bitub.Dto.Scene.ComponentScene SceneModel { get; private set; }

        internal ComponentScene(Bitub.Dto.Scene.ComponentScene componentScene, Qualifier qualifier, Logger logger) : base(qualifier, logger)
        {
            SceneModel = componentScene;
        }

        internal ComponentScene(Bitub.Dto.Scene.ComponentScene componentScene, Logger logger) : base(System.Guid.NewGuid().ToQualifier(), logger)
        {
            SceneModel = componentScene;
        }

        static ComponentScene()
        {
            exportAsFormats = new TRexAssimp.TRexAssimpExport().Formats;
        }

        protected override ComponentScene RequalifyModel(Qualifier qualifier)
        {
            return new ComponentScene(SceneModel, qualifier, Logger);
        }

        protected override LogReason DefaultReason => LogReason.Saved;

        #endregion

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new string FileName => base.FileName;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new string PathName => base.PathName;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new string FormatExtension => base.FormatExtension;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new string CanonicalFileName(string seperator = "-")
        {
            return base.CanonicalFileName(seperator);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new string CanonicalName(string seperator = "-")
        {
            return base.CanonicalName(seperator);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new ComponentScene RelocatePath(string newPathName)
        {
            return base.RelocatePath(newPathName);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new ComponentScene Rename(string fileNameWithoutExt)
        {
            return base.Rename(fileNameWithoutExt);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new ComponentScene RenameWithReplacePattern(string replacePattern, string replaceWith)
        {
            return base.RenameWithReplacePattern(replacePattern, replaceWith);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public new ComponentScene RenameWithSuffix(string fragment)
        {
            return base.RenameWithSuffix(fragment);
        }

#pragma warning restore CS1591

        /// <summary>
        /// Saves the current component model as it is to JSON or binary format.
        /// </summary>
        /// <param name="scene">The scene to save</param>
        /// <param name="formatID">The desired format ID (<see cref="saveAsFormats"/>) to use</param>
        /// <param name="canonicalSeparator">Canonical fragment seperator.</param>
        /// <returns>The saved scene</returns>
        public static ComponentScene Save(ComponentScene scene, string formatID, string canonicalSeparator)
        {
            if (null == scene)
                throw new ArgumentNullException(nameof(scene));

            var format = saveAsFormats.FirstOrDefault(f => f.ID == formatID);
            if (null == format)
                throw new ArgumentException($"Unknown format ID {formatID}.");

            var qualifier = BuildQualifierByExtension(scene.Qualifier, format.Extension);
            var fileName = GetFilePathName(qualifier, canonicalSeparator, true);

            using (var monitor = scene.CreateProgressMonitor(LogReason.Saved))
            {
                try
                {
                    monitor.NotifyProgressEstimateUpdate(1);
                    monitor.NotifyOnProgressChange(0, "Start saving");
                    switch (format.Extension)
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
                            var msg = $"Unknown implementation of format '{format.Extension}'.";
                            scene.OnActionLogged(LogMessage.ByErrorMessage(scene.Name, LogReason.Saved, msg));
                            monitor.State.MarkBroken();

                            throw new ArgumentException(msg);
                    }

                    scene.OnActionLogged(
                        LogMessage.BySeverityAndMessage(scene.Name, LogSeverity.Info, LogReason.Saved, "Scene save to '{0}'.", fileName));
                    monitor.NotifyOnProgressChange(1, "Saved file");
                }
                catch (Exception e)
                {
                    monitor.State.MarkBroken();

                    scene.Logger?.LogError(e, "An exception has been caught: {0}", e.Message);
                    scene.OnActionLogged(
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
        /// <param name="unitScale">The scale</param>
        /// <param name="transform">The axes transform</param>
        /// <param name="formatID">The format ID (one of <see cref="exportAsFormats"/>)</param>
        /// <param name="canonicalSeparator">The canonical fragment separator</param>
        /// <returns>The exported scene</returns>
        public static ComponentScene Export(ComponentScene scene, UnitScale unitScale, CRSTransform transform, string formatID, string canonicalSeparator)
        {
            if (null == scene)
                throw new ArgumentNullException(nameof(scene));

            var format = exportAsFormats.FirstOrDefault(f => f.ID == formatID);
            if (null == format)
                throw new ArgumentException($"Unknown format ID {formatID}.");

            var qualifier = BuildQualifierByExtension(scene.Qualifier, format.Extension);
            var fileName = GetFilePathName(qualifier, canonicalSeparator, true);

            using (var monitor = scene.CreateProgressMonitor(LogReason.Saved))
            {
                try
                {
                    var exp = new TRexAssimp.TRexAssimpExport(new TRexAssimp.TRexAssimpPreferences(transform, unitScale));
                    monitor.NotifyProgressEstimateUpdate(1);
                    monitor.NotifyOnProgressChange(0, "Start exporting");

                    if (!exp.ExportTo(scene.SceneModel, fileName, format))
                    {
                        monitor.State.MarkBroken();
                        scene.OnActionLogged(
                            LogMessage.ByErrorMessage(scene.Name, LogReason.Saved, "An error occured while exporting to '{0}'. {1}", fileName, exp.StatusMessage));
                    }
                    else
                    {
                        monitor.NotifyOnProgressChange(1, "Exported");
                        scene.OnActionLogged(
                            LogMessage.BySeverityAndMessage(scene.Name, LogSeverity.Info, LogReason.Saved, "Scene exported to '{0}'.", fileName));
                    }
                }
                catch (Exception e)
                {
                    monitor.State.MarkBroken();

                    scene.Logger?.LogError(e, "An exception has been caught: {0}", e.Message);
                    scene.OnActionLogged(
                        LogMessage.ByErrorMessage(scene.Name, LogReason.Saved, "Exception '{0}' thrown while exporting to '{1}'.", e.Message, fileName));
                }

                monitor.State.MarkTerminated();
                monitor.NotifyOnProgressEnd();
            }

            return scene;
        }
    }
}
