using Xbim.Common.Geometry;

using Autodesk.DesignScript.Runtime;

namespace Geom
{
    /// <summary>
    /// A 3D point.
    /// </summary>
    public class XYZ
    {
        #region Internals

        /// <summary>
        /// The internal point reference.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public XbimPoint3D XbimPoint { get; set; }

        internal XYZ(double x, double y, double z)
        {
            XbimPoint = new XbimPoint3D(x, y, z);
        }

        #endregion

        /// <summary>
        /// A new IFC cartesian point by given coordinates.
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <returns>A 3D point</returns>
        public static XYZ ByXYZ(double x, double y, double z)
        {
            return new XYZ(x, y, z);
        }
    }
}
