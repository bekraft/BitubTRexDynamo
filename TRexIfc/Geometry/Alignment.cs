using System;

using Autodesk.DesignScript.Runtime;

using Bitub.Ifc.Transform;
using Bitub.Dto.Scene;

using TRex.Log;

namespace TRex.Geom
{
    /// <summary>
    /// Affine alignment between two 3D coordinate systems.
    /// </summary>
    public class Alignment
    {
#pragma warning disable CS1591

        #region Internals

        [IsVisibleInDynamoLibrary(false)]
        public IfcAxisAlignment TheAxisAlignment { get; private set; }

        internal Alignment(IfcAxisAlignment axisAlignment)
        {
            TheAxisAlignment = axisAlignment;
        }

        #endregion


#pragma warning restore CS1591

        /// <summary>
        /// Loads an alignment from persitent file.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <returns>An alignment structure.</returns>
        public static Alignment ByFile(string fileName)
        {
            return new Alignment(IfcAxisAlignment.LoadFromFile(fileName));
        }

        /// <summary>
        /// Will save the current alignment to a persistent file as XML.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <returns>A log message</returns>
        public LogMessage SaveTo(string fileName)
        {
            try
            {
                TheAxisAlignment.SaveToFile(fileName);
            }
            catch(Exception e)
            {
                return LogMessage.BySeverityAndMessage(fileName, LogSeverity.Error, LogReason.Saved, "Error while saving '{0}': {1}", fileName, e.Message);
            }
            return LogMessage.BySeverityAndMessage(fileName, LogSeverity.Info, LogReason.Saved, "Saved '{0}'.", fileName);
        }

        /// <summary>
        /// A new alignment by simply shifting offset from A to B.
        /// </summary>
        /// <param name="offsetA">The source offset.</param>
        /// <param name="offsetB">The target offset</param>
        /// <returns>An alignment structure.</returns>
        public static Alignment ByOffsetShift(XYZ offsetA, XYZ offsetB)
        {
            return new Alignment(new IfcAxisAlignment
            {
                SourceReferenceAxis = new IfcAlignReferenceAxis(offsetA.TheXYZ, offsetA.Translate(1.0f).TheXYZ),
                TargetReferenceAxis = new IfcAlignReferenceAxis(offsetB.TheXYZ, offsetB.Translate(1.0f).TheXYZ)
            });
        }

        /// <summary>
        /// A new alignment by rotating the target CRS pointing into positive X-axis.
        /// </summary>
        /// <param name="xAxis">The positive target X-axis direction</param>        
        /// <returns>An alignment structure.</returns>
        public static Alignment ByXYRotation(XYZ xAxis)
        {
            return new Alignment(new IfcAxisAlignment
            {
                SourceReferenceAxis = new IfcAlignReferenceAxis(),
                TargetReferenceAxis = new IfcAlignReferenceAxis(new Bitub.Dto.Spatial.XYZ(), xAxis.TheXYZ)
            });
        }

        /// <summary>
        /// A 2-by-2 point alignment. Given an offset and orientation point of A and of B, an affine aligment
        /// will be computed. No scaling will be done. The total distance of offset and orientation point is normalized during
        /// calculations.
        /// </summary>
        /// <param name="offsetA">The offset of A</param>
        /// <param name="endA">The orientation point of A (or undefined if simpliy shifted along X)</param>
        /// <param name="offsetB">The offset of B</param>
        /// <param name="endB">The orientation point of B (or undefined if simpliy shifted along X)</param>
        /// <returns>An alignment structure.</returns>
        public static Alignment By2x2Reference(XYZ offsetA, XYZ endA, XYZ offsetB, XYZ endB)
        {
            return new Alignment(new IfcAxisAlignment
            {
                SourceReferenceAxis = new IfcAlignReferenceAxis(offsetA.TheXYZ, endA?.TheXYZ ?? offsetA.Translate(1).TheXYZ),
                TargetReferenceAxis = new IfcAlignReferenceAxis(offsetB.TheXYZ, endB?.TheXYZ ?? offsetB.Translate(1).TheXYZ)
            });
        }
    }
}
