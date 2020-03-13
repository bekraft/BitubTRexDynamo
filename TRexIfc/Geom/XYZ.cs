using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xbim.Common.Geometry;

namespace TRexIfc.Geom
{
    /// <summary>
    /// A 3D point.
    /// </summary>
    public class XYZ
    {
        #region Internals

        internal XbimPoint3D XbimPoint { get; set; }

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
