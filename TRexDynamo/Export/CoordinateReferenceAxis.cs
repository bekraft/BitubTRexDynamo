using System;

using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;

using Autodesk.DesignScript.Runtime;

namespace TRexDynamo.Export
{
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

    [IsVisibleInDynamoLibrary(false)]
    public sealed class CoordinateReferenceAxis
    {
        public readonly static CoordinateReferenceAxis[] given = new[]
        {
            new CoordinateReferenceAxis("Default",
                GlobalReferenceAxis.PositiveX, GlobalReferenceAxis.PositiveY, GlobalReferenceAxis.PositiveZ),

            new CoordinateReferenceAxis("Y-Z or Z-Y", 
                GlobalReferenceAxis.PositiveX, GlobalReferenceAxis.PositiveZ, GlobalReferenceAxis.PositiveY),
        };

        #region Internals

        // Rotation matrix given by 3 axis
        private Rotation matrix3;
        private GlobalReferenceAxis[] globalAxes;

        /// <summary>
        /// New CRS based on reference exactly 3 axes. No algebraic checks are performed. Validity 
        /// is assumed by intention.
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="right">The right direction</param>
        /// <param name="forward">The forward direction</param>
        /// <param name="up">The up direction</param>
        internal CoordinateReferenceAxis(string name, GlobalReferenceAxis right, GlobalReferenceAxis up, GlobalReferenceAxis forward)
        {
            matrix3 = new Rotation
            {
                Rx = GetUnitAxis(right),
                Ry = GetUnitAxis(up),
                Rz = GetUnitAxis(forward),
            };

            globalAxes = new GlobalReferenceAxis[] { right, up, forward };

            Name = name;
        }

        internal CoordinateReferenceAxis(string name, Rotation reference)
        {
            Name = name;

            matrix3 = new Rotation(reference);
            globalAxes = new GlobalReferenceAxis[0];
        }

        #endregion

        public string Name { get; private set; }

        public Rotation Rotation 
        {
            get => new Rotation(matrix3);
        }

        public bool IsGlobal
        {
            get => globalAxes.Length == 3;
        }

        public GlobalReferenceAxis[] GlobalReferenceAxes
        {
            get => globalAxes;
        }

        public XYZ GetUnitAxis(GlobalReferenceAxis referenceAxis)
        {
            int axis = (int)referenceAxis;
            switch (Math.Abs(axis))
            {
                case 1:
                    return new XYZ(Math.Sign(axis) * 1, 0, 0);
                case 2:
                    return new XYZ(0, Math.Sign(axis) * 1, 0);
                case 3:
                    return new XYZ(0, 0, Math.Sign(axis) * 1);
            }
            throw new NotSupportedException();
        }
    }
}
