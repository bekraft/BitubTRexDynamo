
using Bitub.Dto.Spatial;
//using SpatialXYZ = Bitub.Dto.Spatial.XYZ;

namespace Geom
{
    /// <summary>
    /// An axis aligned bounding box region with two min and max points and a weigtht.
    /// </summary>
    public class AABBRegion
    {
        #region Internals

        internal ABox TheABox { get; set; }

        internal AABBRegion(float dx, float dy, float dz)
        {
            TheABox = new ABox
            {
                Min = new Bitub.Dto.Spatial.XYZ(),
                Max = new Bitub.Dto.Spatial.XYZ
                {
                    X = dx,
                    Y = dy,
                    Z = dz
                }
            };
        }

        internal AABBRegion(ABox aBox)
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
            return new AABBRegion(new ABox { Min = min.TheXYZ, Max = max.TheXYZ }) { Weight = weight };
        }
    }
}
