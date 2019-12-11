using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xbim.Common.Geometry;

namespace TRexIfc.Geom
{
    /// <summary>
    /// IFC cartesian point.
    /// </summary>
    public class IfcCartesianPoint
    {
        internal XbimPoint3D XYZ { get; set; }

        internal IfcCartesianPoint(double x, double y, double z)
        {
            XYZ = new XbimPoint3D(x, y, z);
        }

        /// <summary>
        /// A new IFC cartesian point by given coordinates.
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <returns>A 3D point</returns>
        public static IfcCartesianPoint ByXYZ(double x, double y, double z)
        {
            return new IfcCartesianPoint(x, y, z);
        }
    }
}
