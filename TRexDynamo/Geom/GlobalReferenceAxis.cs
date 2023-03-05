using Autodesk.DesignScript.Runtime;

namespace TRex.Geom
{
    /// <summary>
    /// Global reference axis identifiers.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public enum GlobalReferenceAxis
    {
        NegativeX = -1,
        NegativeY = -2,
        NegativeZ = -3,

        PositiveX = 1,
        PositiveY = 2,
        PositiveZ = 3    
    }

    public static class GlobalReferenceAxisExtensions
    {
        [IsVisibleInDynamoLibrary(false)]
        public static GlobalReferenceAxis Invert(this GlobalReferenceAxis axis)
        {
            return (GlobalReferenceAxis)((int)axis * -1);
        }
    }
}
