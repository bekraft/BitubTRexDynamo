using Xbim.Common.Geometry;

using Autodesk.DesignScript.Runtime;
using ADPoint = Autodesk.DesignScript.Geometry.Point;

namespace Geom
{
    /// <summary>
    /// An analytical 3D point or vector construct.
    /// </summary>
    public class XYZ
    {
        #region Internals

#pragma warning disable CS1591

        /// <summary>
        /// The internal point reference.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public Bitub.Transfer.Spatial.XYZ Point { get; set; }

        internal XYZ() : this(0, 0, 0)
        { }

        internal XYZ(double x, double y, double z)
        {
            Point = new Bitub.Transfer.Spatial.XYZ { X = x, Y = y, Z = z };
        }

        [IsVisibleInDynamoLibrary(false)]
        public XbimPoint3D ToXbimPoint3D()
        {
            return new XbimPoint3D(Point.X, Point.Y, Point.Z);
        }

        [IsVisibleInDynamoLibrary(false)]
        public XbimVector3D ToXbimVector3D()
        {
            return new XbimVector3D(Point.X, Point.Y, Point.Z);
        }

#pragma warning restore CS1591

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

        /// <summary>
        /// A new XYZ vector as difference between target and origin.
        /// </summary>
        /// <param name="origin">The base point of vector</param>
        /// <param name="target">The target point of vector</param>
        /// <returns>A XYZ object as vector</returns>
        public static XYZ ByVector(XYZ origin, XYZ target)
        {
            return new XYZ(target.Point.X - origin.Point.X, target.Point.Y - origin.Point.Y, target.Point.Z - origin.Point.Z);
        }

        /// <summary>
        /// Wraps the analytical XYZ into a visual point.
        /// </summary>
        /// <returns>An AutoDesk design script point structure</returns>
        public ADPoint ToPoint()
        {
            return ADPoint.ByCoordinates(Point.X, Point.Y, Point.Z);
        }
    }
}
