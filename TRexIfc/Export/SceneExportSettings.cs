using System;
using System.Linq;

using Bitub.Ifc.Export;
using Bitub.Dto;    
using Bitub.Dto.Scene;

using Autodesk.DesignScript.Runtime;

using Geom;
using Log;
using Data;

namespace Export
{
    /// <summary>
    /// Scene export settings
    /// </summary>
    public class SceneExportSettings
    {
#pragma warning disable CS1591

        #region Internals

        internal SceneExportSettings() : this(new ExportPreferences())
        { }

        internal SceneExportSettings(ExportPreferences preferences)
        {
            Preferences = preferences;
        }

        [IsVisibleInDynamoLibrary(false)]
        public readonly ExportPreferences Preferences;

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
        /// <param name="featureClassificationFilter">A feature filter passing feature to be classifications</param>
        /// <returns>New scene export settings</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static SceneExportSettings ByParameters(string transformationStrategy,
            string positioningStrategy,
            XYZ offset,
            float unitPerMeter,
            string[] sceneContexts,
            CanonicalFilter featureClassificationFilter)
        {
            if (string.IsNullOrEmpty(transformationStrategy) || string.IsNullOrEmpty(positioningStrategy))
                throw new ArgumentNullException("transformationStrategy | positioningStrategy");

            var settings = new SceneExportSettings(new ExportPreferences
            {
                Transforming = (SceneTransformationStrategy)Enum.Parse(typeof(SceneTransformationStrategy), transformationStrategy, true),
                Positioning = (ScenePositioningStrategy)Enum.Parse(typeof(ScenePositioningStrategy), positioningStrategy, true),
                UserModelCenter = offset.TheXYZ,
                UnitsPerMeter = unitPerMeter,                
                SelectedContext = sceneContexts?.Select(c => new SceneContext { Name = c.ToQualifier() }).ToArray() ?? new SceneContext[] { },
                FeatureToClassifierFilter = featureClassificationFilter?.Filter
            });

            return settings;
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
        public string TransformationStrategy { get => Enum.GetName(typeof(SceneTransformationStrategy), Preferences.Transforming); }

        /// <summary>
        /// The positioning strategy.
        /// </summary>
        public string PositioningStrategy { get => Enum.GetName(typeof(ScenePositioningStrategy), Preferences.Positioning); }

        /// <summary>
        /// The scaling as units per meter.
        /// </summary>
        public double UnitsPerMeter { get => Preferences.UnitsPerMeter; }

        /// <summary>
        /// The model offset in meter.
        /// </summary>
        public XYZ Offset { get => new XYZ { TheXYZ = Preferences.UserModelCenter }; }

        /// <summary>
        /// Names of model contexts to be exported.
        /// </summary>
        public Canonical[] SceneContexts { get => Preferences.SelectedContext.Select(c => new Canonical(c.Name)).ToArray(); }
    }
}
