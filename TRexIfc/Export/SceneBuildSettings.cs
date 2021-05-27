using System;
using System.Linq;

using Bitub.Ifc.Export;
using Bitub.Dto;    
using Bitub.Dto.Scene;

using Autodesk.DesignScript.Runtime;

using TRex.Geom;
using TRex.Log;
using TRex.Data;

namespace TRex.Export
{
    /// <summary>
    /// Scene export settings
    /// </summary>
    public sealed class SceneBuildSettings
    {
#pragma warning disable CS1591

        #region Internals

        internal SceneBuildSettings() : this(new ExportPreferences())
        { }

        internal SceneBuildSettings(ExportPreferences preferences)
        {
            Preferences = preferences;
        }

        [IsVisibleInDynamoLibrary(false)]
        public ExportPreferences Preferences { get; private set; }

        #endregion

        [IsVisibleInDynamoLibrary(false)]
        public static SceneBuildSettings ByContext(params string[] contexts)
        {
            return new SceneBuildSettings(new ExportPreferences
            {
                Transforming = SceneTransformationStrategy.Quaternion,
                Positioning = ScenePositioningStrategy.NoCorrection,
                UserModelCenter = new Bitub.Dto.Spatial.XYZ(),
                UnitsPerMeter = 1.0f,
                SelectedContext = contexts?.Select(c => new SceneContext { Name = c.ToQualifier() }).ToArray() ?? new SceneContext[] { },
                FeatureToClassifierFilter = null
            });
        }

        [IsVisibleInDynamoLibrary(false)]
        public static SceneBuildSettings ByParameters(string transformationStrategy,
            string positioningStrategy,
            XYZ offset,
            UnitScale unitScale,
            string[] sceneContexts,
            CanonicalFilter featureClassificationFilter)
        {
            if (string.IsNullOrEmpty(transformationStrategy) || string.IsNullOrEmpty(positioningStrategy))
                throw new ArgumentNullException("transformationStrategy | positioningStrategy");

            var settings = new SceneBuildSettings(new ExportPreferences
            {
                Transforming = (SceneTransformationStrategy)Enum.Parse(typeof(SceneTransformationStrategy), transformationStrategy, true),
                Positioning = (ScenePositioningStrategy)Enum.Parse(typeof(ScenePositioningStrategy), positioningStrategy, true),
                UserModelCenter = offset.TheXYZ,
                UnitsPerMeter = unitScale?.UnitsPerMeter ?? 1,
                SelectedContext = sceneContexts?.Select(c => new SceneContext { Name = c.ToQualifier() }).ToArray() ?? new SceneContext[] { },
                FeatureToClassifierFilter = featureClassificationFilter?.Filter
            });

            return settings;
        }

#pragma warning restore CS1591

        /// <summary>
        /// Saves the settings to file as XML.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="settings">The settings</param>
        /// <returns>A log message</returns>
        public static LogMessage SaveAs(SceneBuildSettings settings, string fileName)
        {
            try
            {
                settings.Preferences.SaveTo(fileName);
                return LogMessage.BySeverityAndMessage(fileName, LogSeverity.Info, LogReason.Saved, "Saved {0}", fileName);
            }
            catch(Exception e)
            {
                return LogMessage.BySeverityAndMessage(fileName, LogSeverity.Error, LogReason.Saved, "{0}: {1} ({2})", e, e.Message, fileName);
            }
        }

        /// <summary>
        /// The transformation strategy.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public string TransformationStrategy { get => Enum.GetName(typeof(SceneTransformationStrategy), Preferences.Transforming); }

        /// <summary>
        /// The positioning strategy.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public string PositioningStrategy { get => Enum.GetName(typeof(ScenePositioningStrategy), Preferences.Positioning); }

        /// <summary>
        /// The scaling as units per meter.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public double UnitsPerMeter { get => Preferences.UnitsPerMeter; }

        /// <summary>
        /// The model offset in meter.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public XYZ Offset { get => new XYZ { TheXYZ = Preferences.UserModelCenter }; }

        /// <summary>
        /// Names of model contexts to be exported.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public Canonical[] SceneContexts { get => Preferences.SelectedContext.Select(c => new Canonical(c.Name)).ToArray(); }
    }
}
