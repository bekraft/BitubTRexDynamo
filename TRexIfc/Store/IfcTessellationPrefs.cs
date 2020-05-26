using System;
using System.Collections.Generic;

using Autodesk.DesignScript.Runtime;

using System.Xml.Serialization;
using Xbim.Common;

namespace Store
{
    /// <summary>
    /// Tessellation preferences and tolerances.
    /// </summary>
    [XmlRoot("IfcTessellationPrefs", Namespace = "https://github.com/bekraft/BitubTRexDynamo/Store")]
    public class IfcTessellationPrefs
    {
        #region Internals

#pragma warning disable CS1591

        /// <summary>
        /// Count of model units to assemble 1.0 SI meter.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public double OneMeter { get; set; }

        [IsVisibleInDynamoLibrary(false)]
        public double DeflectionTolerance { get; set; }
        [IsVisibleInDynamoLibrary(false)]
        public double DeflectionAngle { get; set; }
        [IsVisibleInDynamoLibrary(false)]
        public double Precision { get; set; }
        [IsVisibleInDynamoLibrary(false)]
        public double PrecisionMax { get; set; }
        [IsVisibleInDynamoLibrary(false)]
        public double PrecisionBoolean { get; set; }
        [IsVisibleInDynamoLibrary(false)]
        public double PrecisionBooleanMax { get; set; }

        internal IfcTessellationPrefs()
        {            
        }

        internal void ApplyTo(IModel model)
        {
            // Rescale to model conversion length unit
            var mf = model.ModelFactors;

            mf.DeflectionAngle = DeflectionAngle;
            mf.DeflectionTolerance = DeflectionTolerance * mf.OneMeter;
            mf.Precision = Precision * mf.OneMeter;
            mf.PrecisionMax = PrecisionMax * mf.OneMeter;
            mf.PrecisionBoolean = PrecisionBoolean * mf.OneMeter;
            mf.PrecisionBooleanMax = PrecisionBooleanMax * mf.OneMeter;
            model.GeometryStore?.Dispose();
        }

#pragma warning restore CS1591

        #endregion

        /// <summary>
        /// Applies the preferences to an IFC model instance.
        /// </summary>
        /// <param name="ifcModel">The IFC model</param>
        /// <returns>Modified IFC model</returns>
        public IfcModel ApplyToModel(IfcModel ifcModel)
        {
            if (null == ifcModel)
                throw new ArgumentException("No ifcModel");

            ApplyTo(ifcModel.Store.XbimModel);
            return ifcModel;
        }

        /// <summary>
        /// Returns the model internal defaults.
        /// </summary>
        /// <param name="ifcModel">The IFC model</param>
        /// <returns>Current tesselation preferences</returns>
        public static IfcTessellationPrefs ByModelDefaults(IfcModel ifcModel)
        {
            var internalModel = ifcModel?.Store.XbimModel;
            if (null == internalModel)
                throw new ArgumentNullException("ifcModel");

            var mf = internalModel.ModelFactors;
            return new IfcTessellationPrefs
            {
                OneMeter = mf.OneMeter,
                Precision = mf.Precision * mf.LengthToMetresConversionFactor,
                PrecisionMax = mf.PrecisionMax * mf.LengthToMetresConversionFactor,
                PrecisionBoolean = mf.PrecisionBoolean * mf.LengthToMetresConversionFactor,
                PrecisionBooleanMax = mf.PrecisionBooleanMax * mf.LengthToMetresConversionFactor,
                DeflectionAngle = mf.DeflectionAngle,
                DeflectionTolerance = mf.DeflectionTolerance * mf.LengthToMetresConversionFactor
            };
        }

        /// <summary>
        /// Returns a separate values of preferences.
        /// </summary>
        /// <returns>The settings</returns>
        public Dictionary<string, double> ToValues()
        {
            return new Dictionary<string, double>() 
            {
                { "UnitsPerMeter", OneMeter },
                { "PrecisionPerMeter", Precision },
                { "PrecisionMaxPerMeter", PrecisionMax },
                { "PrecisionBooleanPerMeter", PrecisionBoolean },
                { "PrecisionBooleanMaxPerMeter", PrecisionBooleanMax },
                { "DeflectionTolerancePerMeter", DeflectionTolerance },
                { "DeflectionAngleDeg", DeflectionAngle }
            };
        }

        /// <summary>
        /// Defines a new tessellation preference by given tolerances.
        /// </summary>
        /// <param name="precPerMeter">The leading precision in meter</param>
        /// <param name="precMaxPerMeter">The lowest precision in meter</param>
        /// <param name="precBooleaonPerMeter">Boolean ops leading precision in meter</param>
        /// <param name="precBooleanMaxPerMeter">Boolean ops lowest precision in meter</param>
        /// <param name="deflectionTolPerMeter">Deflection tolerance on higher curves in meter</param>
        /// <param name="deflectionAngleInDeg">Max deflection angle on higher curves in degree</param>
        /// <returns>Preference settings</returns>
        public static IfcTessellationPrefs ByPreferences(
            double precPerMeter,
            double precMaxPerMeter,
            double precBooleaonPerMeter,
            double precBooleanMaxPerMeter,
            double deflectionTolPerMeter,
            double deflectionAngleInDeg)
        {
            return new IfcTessellationPrefs
            {
                OneMeter = 1.0,
                Precision = precPerMeter,
                PrecisionMax = precMaxPerMeter,
                PrecisionBoolean = precBooleaonPerMeter,
                PrecisionBooleanMax = precBooleanMaxPerMeter,
                DeflectionTolerance = deflectionTolPerMeter,
                DeflectionAngle = deflectionAngleInDeg
            };
        }
    }
}
