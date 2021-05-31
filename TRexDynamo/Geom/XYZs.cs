using System;

using Bitub.Dto.Spatial;

using Autodesk.DesignScript.Geometry;

using TRex.Internal;

namespace TRex.Geom
{
    /// <summary>
    /// A XYZ factory and library.
    /// </summary>
    public sealed class XYZs
    {
#pragma warning disable CS1591

        #region Internals

        private XYZs()
        { }

        #endregion

#pragma warning restore CS1591

        /// <summary>
        /// A new IFC cartesian point by given coordinates.
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <returns>A 3D point</returns>
        public static XYZ ByXYZ(float x, float y, float z)
        {
            return new XYZ(x, y, z);
        }

        /// <summary>
        /// A new array of XYZ by given cooridinates.
        /// </summary>
        /// <param name="xyz">The coordinate list</param>
        /// <returns>The XYZ list</returns>
        public static XYZ[] ByList(float[] xyz)
        {
            if (0 != xyz.Length % 3)
                throw new ArgumentException($"Expecting a double list of a length module 3");

            var xyzs = new XYZ[xyz.Length / 3];
            for (int i=0; i < xyzs.Length; i++)
            {
                xyzs[i] = new XYZ(xyz[i * 3], xyz[i * 3 + 1], xyz[i * 3 + 2]);
            }
            return xyzs;
        }

        /// <summary>
        /// A new XYZ vector as difference between target and origin.
        /// </summary>
        /// <param name="origin">The base point of vector</param>
        /// <param name="target">The target point of vector</param>
        /// <returns>A XYZ object as vector</returns>
        public static XYZ ByDiff(XYZ origin, XYZ target)
        {            
            return target.Sub(origin);
        }

        /// <summary>
        /// Wraps the analytical XYZ into a visual point.
        /// </summary>
        /// <returns>An AutoDesk design script point structure</returns>
        public static Point ToPoint(XYZ xyz)
        {
            try
            {
                return Point.ByCoordinates(xyz.X, xyz.Y, xyz.Z);
            }
            catch(Exception e)
            {
                GlobalLogging.log.Error(e, "Point class hasn't been found. Maybe a library reference is missing.");
                throw new ArgumentException(e.Message);
            }
        }

        /// <summary>
        /// Translates the XYZ coordinate by given offsets.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>A new XYZ</returns>
        public static XYZ ByAdd(XYZ xyz, float x, float y, float z)
        {
            return new XYZ(xyz.X + x, xyz.Y + y, xyz.Z + z);
        }

        /// <summary>
        /// Translate via given vector.
        /// </summary>
        /// <param name="vector">The vector</param>
        /// <returns>Returns a new translated XYZ</returns>
        public static XYZ ByAdd(XYZ xyz, XYZ vector)
        {
            return xyz.Add(vector);
        }

        /// <summary>
        /// Returns a mean point of given points.
        /// </summary>
        /// <param name="points">The points</param>
        /// <returns>A new mean point</returns>
        public static XYZ ByMean(XYZ[] points)
        {
            float x = 0, y = 0, z = 0;
            foreach (var xyz in points)
            {
                x += xyz.X;
                y += xyz.Y;
                z += xyz.Z;
            }

            return new XYZ(x / points.Length, y / points.Length, z / points.Length);
        }
    }
}
