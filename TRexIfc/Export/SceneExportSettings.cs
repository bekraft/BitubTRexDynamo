using System;
using System.Linq;

using Bitub.Ifc.Scene;
using Bitub.Transfer.Scene;

using Autodesk.DesignScript.Runtime;

using Geom;
using Log;

namespace Export
{
    /// <summary>
    /// Scene export settings
    /// </summary>
    public class SceneExportSettings
    {
#pragma warning disable CS1591

        #region Internals

        internal SceneExportSettings() : this(new IfcSceneExportSettings())
        { }

        internal SceneExportSettings(IfcSceneExportSettings settings)
        {
            InternalSettings = settings;
        }

        [IsVisibleInDynamoLibrary(false)]
        public IfcSceneExportSettings InternalSettings { get; private set; }

        #endregion

#pragma warning restore CS1591

        /// <summary>
        /// Creates settings by given arguments.
        /// </summary>
        /// <param name="transformationStrategy">See <see cref="SceneTransformationStrategy"/></param>
        /// <param name="positioningStrategy">See <see cref="PositioningStrategy"/></param>
        /// <param name="offset">The offset coordinates</param>
        /// <param name="unitPerMeter">The units per meter</param>
        /// <param name="sceneContexts">The scene context(s) to be exported</param>
        /// <param name="logger">The logging instance</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static SceneExport BySettings(string transformationStrategy,
            string positioningStrategy,
            XYZ offset,
            double unitPerMeter,
            string[] sceneContexts,
            Logger logger)
        {
            if (string.IsNullOrEmpty(transformationStrategy) || string.IsNullOrEmpty(positioningStrategy))
                throw new ArgumentNullException("transformationStrategy | positioningStrategy");

            var settings = new SceneExportSettings
            {
                InternalSettings = new IfcSceneExportSettings
                {
                    Transforming = (SceneTransformationStrategy)Enum.Parse(typeof(SceneTransformationStrategy), transformationStrategy, true),
                    Positioning = (ScenePositioningStrategy)Enum.Parse(typeof(ScenePositioningStrategy), positioningStrategy, true),
                    UserModelCenter = offset.TheXYZ,
                    UnitsPerMeter = unitPerMeter,
                    UserRepresentationContext = sceneContexts?.Select(c => new SceneContext { Name = c }).ToArray() ?? new SceneContext[] {}
                }
            };
            return SceneExport.CreateSceneExport(settings, logger);
        }

        /// <summary>
        /// Saves the settings as a template file.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="settings">The settings</param>
        /// <returns>A log message</returns>
        public static LogMessage SaveAs(SceneExportSettings settings, string fileName)
        {
            try
            {
                settings.InternalSettings.SaveTo(fileName);
                return LogMessage.BySeverityAndMessage(Severity.Info, ActionType.Saved, "Saved {0}", fileName);
            }
            catch(Exception e)
            {
                return LogMessage.BySeverityAndMessage(Severity.Error, ActionType.Saved, "{0}: {1} ({2})", e, e.Message, fileName);
            }
        }

        /// <summary>
        /// The transformation strategy.
        /// </summary>
        public string TransformationStrategy { get => Enum.GetName(typeof(SceneTransformationStrategy), InternalSettings.Transforming); }

        /// <summary>
        /// The positioning strategy.
        /// </summary>
        public string PositioningStrategy { get => Enum.GetName(typeof(ScenePositioningStrategy), InternalSettings.Positioning); }

        /// <summary>
        /// The scaling as units per meter.
        /// </summary>
        public double UnitsPerMeter { get => InternalSettings.UnitsPerMeter; }

        /// <summary>
        /// The offset shift
        /// </summary>
        public XYZ Offset { get => new XYZ { TheXYZ = InternalSettings.UserModelCenter }; }

        /// <summary>
        /// Names of model contexts to be exported.
        /// </summary>
        public string[] SceneContexts { get => InternalSettings.UserRepresentationContext.Select(c => c.Name).ToArray(); }
    }
}
