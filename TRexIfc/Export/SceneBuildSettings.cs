using System;
using System.Linq;

using Bitub.Dto;    
using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;

using Bitub.Xbim.Ifc.Export;

using Autodesk.DesignScript.Runtime;

using TRex.Geom;
using TRex.Log;
using TRex.Data;
using TRex.Internal;

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
                UserModelCenter = new XYZ(),
                Scale = XYZ.Ones,
                CRS = Transform.Identity,
                SelectedContext = contexts?.Select(c => new SceneContext { Name = c.ToQualifier() }).ToArray() ?? new SceneContext[] { },
                ComponentIdentificationStrategy = SceneComponentIdentificationStrategy.UseGloballyUniqueID
            });
        }

        [IsVisibleInDynamoLibrary(false)]
        public static SceneBuildSettings ByParameters(string transformationStrategy,
            string positioningStrategy,
            XYZ userModelCenter,
            CRSTransform crs,
            UnitScale unitScale,
            string[] sceneContexts,
            string identificationStrategy)
        {
            if (string.IsNullOrEmpty(transformationStrategy) || string.IsNullOrEmpty(positioningStrategy) || string.IsNullOrEmpty(identificationStrategy))
                throw new ArgumentNullException("transformationStrategy | positioningStrategy | identificationStrategy");

            var settings = new SceneBuildSettings(new ExportPreferences
            {
                Transforming = DynamicArgumentDelegation.TryCastEnumOrDefault<SceneTransformationStrategy>(transformationStrategy),
                Positioning = DynamicArgumentDelegation.TryCastEnumOrDefault<ScenePositioningStrategy>(positioningStrategy),
                UserModelCenter = userModelCenter,
                CRS = crs.Transform,
                Scale = XYZ.Ones * (unitScale?.UnitsPerMeter ?? 1),
                SelectedContext = sceneContexts?.Select(c => new SceneContext { Name = c.ToQualifier() }).ToArray() ?? new SceneContext[] { },
                ComponentIdentificationStrategy = DynamicArgumentDelegation.TryCastEnumOrDefault<SceneComponentIdentificationStrategy>(identificationStrategy)
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
    }
}
