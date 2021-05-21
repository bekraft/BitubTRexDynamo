using Autodesk.DesignScript.Runtime;

namespace TRexDynamo.Export
{
    [IsVisibleInDynamoLibrary(false)]
    public sealed class UnitFactor
    {
        /// <summary>
        /// Given unit factors
        /// </summary>
        public readonly static UnitFactor[] given =
        {
            new UnitFactor("m", 1.0f),
            new UnitFactor("mm", 1000.0f),
            new UnitFactor("cm", 100.0f),
            new UnitFactor("in", 1000.0f / 25.4f), // 25.4mm per inch
            new UnitFactor("ft", 1000.0f / (12 * 25.4f)) // 12 inch per foot                                                   
        };

        #region Internals

        internal UnitFactor(string name, float scaleToMeter)
        {
            Name = name;
            ScaleToMeter = scaleToMeter;
        }

        #endregion

        /// <summary>
        /// Gets the scale of this unit per meter.
        /// </summary>
        public float ScaleToMeter { get; private set; }

        /// <summary>
        /// The name of unit.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns a scaled count of units per meter.
        /// </summary>
        /// <param name="unitsPerMeter"></param>
        /// <returns></returns>
        public float GetUnitsPerMeter(float unitsPerMeter)
        {
            return unitsPerMeter * ScaleToMeter;
        }
    }
}
