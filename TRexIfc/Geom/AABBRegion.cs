using Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SpatialABox = Bitub.Transfer.Spatial.ABox;
using SpatialXYZ = Bitub.Transfer.Spatial.XYZ;

namespace Geom
{
    /// <summary>
    /// An axis aligned bounding box region with two min and max points and a weigtht.
    /// </summary>
    public class AABBRegion
    {
        #region Internals

        internal SpatialABox TheABox { get; set; }

        internal AABBRegion(double dx, double dy, double dz)
        {
            TheABox = new SpatialABox
            {
                Min = new SpatialXYZ(),
                Max = new SpatialXYZ
                {
                    X = dx,
                    Y = dy,
                    Z = dz
                }
            };
        }

        internal AABBRegion(SpatialABox aBox)
        {
            TheABox = aBox;
        }

        #endregion

        /// <summary>
        /// The associated weight.
        /// </summary>
        public double Weight
        {
            get;
            internal set;
        }

        /// <summary>
        /// A new region by given min and max extents.
        /// </summary>
        /// <param name="min">The lower coordinates</param>
        /// <param name="max">The upper coordinates</param>
        /// <param name="weight">An optional weight</param>
        /// <returns>A new region</returns>
        public static AABBRegion ByExtent(XYZ min, XYZ max, double weight = 1.0)
        {
            return new AABBRegion(new SpatialABox { Min = min.TheXYZ, Max = max.TheXYZ }) { Weight = weight };
        }
    }
}
