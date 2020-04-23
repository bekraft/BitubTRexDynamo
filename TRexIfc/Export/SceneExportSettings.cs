using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bitub.Ifc.Scene;
using Bitub.Transfer.Spatial;

using Autodesk.DesignScript.Runtime;
using Bitub.Transfer.Scene;

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
        public static SceneExportSettings BySettings(SceneTransformationStrategy transformationStrategy,
            ScenePositioningStrategy positioningStrategy,
            XYZ offset, 
            double unitPerMeter, 
            string[] sceneContexts)
        {
            return new SceneExportSettings
            {
                Settings = new IfcSceneExportSettings
                {
                    Transforming = transformationStrategy,
                    Positioning = positioningStrategy,
                    UserModelCenter = offset,
                    UnitsPerMeter = unitPerMeter,
                    UserRepresentationContext = sceneContexts.Select(c => new SceneContext { Name = c }).ToArray()
                }
            };
        }

        [IsVisibleInDynamoLibrary(false)]
        public IfcSceneExportSettings Settings { get; private set; }

        #endregion

#pragma warning restore CS1591

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
        public SceneTransformationStrategy TransformationStrategy { get => Settings.Transforming; }

        /// <summary>
        /// The positioning strategy.
        /// </summary>
        public ScenePositioningStrategy PositioningStrategy { get => Settings.Positioning; }

        /// <summary>
        /// The scaling as units per meter.
        /// </summary>
        public double UnitsPerMeter { get => Settings.UnitsPerMeter; }

        /// <summary>
        /// The offset shift
        /// </summary>
        public XYZ Offset { get => Settings.UserModelCenter; }

        /// <summary>
        /// Names of model contexts to be exported.
        /// </summary>
        public string[] SceneContexts { get => Settings.UserRepresentationContext.Select(c => c.Name).ToArray(); }
    }
}
