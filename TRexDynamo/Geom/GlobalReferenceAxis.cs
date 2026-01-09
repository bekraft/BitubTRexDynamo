using System;
using Autodesk.DesignScript.Runtime;
using Bitub.Dto.Spatial;

namespace TRex.Geom
{
    /// <summary>
    /// Global reference axis identifiers.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public enum GlobalReferenceAxis
    {
        X = 0,
        Y = 1,
        Z = 2    
    }
    
    ///<summary>
    ///GlobalReferenceAxis extensions.
    ///</summary>
    public static class GlobalReferenceAxisExtensions
    {
        /// <summary>
        /// Get the unit axis vector according to given global reference axis.
        /// </summary>
        /// <param name="referenceAxis">The global reference</param>
        /// <returns>A unit vector</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static XYZ ToUnitAxis(GlobalReferenceAxis referenceAxis)
        {
            var axis = (int)referenceAxis;
            return Math.Abs(axis) switch
            {
                1 => new XYZ(Math.Sign(axis), 0, 0),
                2 => new XYZ(0, Math.Sign(axis), 0),
                3 => new XYZ(0, 0, Math.Sign(axis)),
                _ => throw new NotSupportedException()
            };
        }
    }
}
