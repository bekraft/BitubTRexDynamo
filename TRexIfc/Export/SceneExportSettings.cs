using System;
using System.Linq;

using Bitub.Ifc.Scene;
using Bitub.Transfer.Scene;

using Autodesk.DesignScript.Runtime;

using Geom;

namespace Export
{
    /// <summary>
    /// Scene export settings
    /// </summary>
    public class SceneExportSettings
    {
#pragma warning disable CS1591

        #region Internals

        internal SceneExportSettings()
        { }

        [IsVisibleInDynamoLibrary(false)]
        public IfcSceneExportSettings Settings { get; private set; }

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
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(true)]
        public static SceneExportSettings BySettings(string transformationStrategy,
            string positioningStrategy,
            XYZ offset,
            double unitPerMeter,
            string[] sceneContexts)
        {
            return new SceneExportSettings
            {
                Settings = new IfcSceneExportSettings
                {
                    Transforming = (SceneTransformationStrategy)Enum.Parse(typeof(SceneTransformationStrategy), transformationStrategy, true),
                    Positioning = (ScenePositioningStrategy)Enum.Parse(typeof(ScenePositioningStrategy), positioningStrategy, true),
                    UserModelCenter = offset.TheXYZ,
                    UnitsPerMeter = unitPerMeter,
                    UserRepresentationContext = sceneContexts?.Select(c => new SceneContext { Name = c }).ToArray() ?? new SceneContext[] {}
                }
            };
        }

        /// <summary>
        /// Reads settings from persitent configuration file.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <returns>An export setting</returns>
        public static SceneExportSettings ByFileName(string fileName)
        {
            return new SceneExportSettings
            {
                Settings = IfcSceneExportSettings.ReadFrom(fileName)
            };
        }

        /// <summary>
        /// The transformation strategy.
        /// </summary>
        public string TransformationStrategy { get => Enum.GetName(typeof(SceneTransformationStrategy), Settings.Transforming); }

        /// <summary>
        /// The positioning strategy.
        /// </summary>
        public string PositioningStrategy { get => Enum.GetName(typeof(ScenePositioningStrategy), Settings.Positioning); }

        /// <summary>
        /// The scaling as units per meter.
        /// </summary>
        public double UnitsPerMeter { get => Settings.UnitsPerMeter; }

        /// <summary>
        /// The offset shift
        /// </summary>
        public XYZ Offset { get => new XYZ { TheXYZ = Settings.UserModelCenter }; }

        /// <summary>
        /// Names of model contexts to be exported.
        /// </summary>
        public string[] SceneContexts { get => Settings.UserRepresentationContext.Select(c => c.Name).ToArray(); }
    }
}
