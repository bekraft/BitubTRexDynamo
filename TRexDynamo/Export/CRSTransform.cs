using System;

using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;

using Autodesk.DesignScript.Runtime;

namespace TRex.Export
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
    public sealed class CRSTransform
    {
        public readonly static CRSTransform[] defined = new[]
        {
            new CRSTransform("Default", // None
                GlobalReferenceAxis.PositiveX, GlobalReferenceAxis.PositiveY, GlobalReferenceAxis.PositiveZ),

            new CRSTransform("Swap Y-Z", // LHS <=> RHS
                GlobalReferenceAxis.PositiveX, GlobalReferenceAxis.PositiveZ, GlobalReferenceAxis.PositiveY),

            new CRSTransform("Mirror Y", // LHS <=> RHS
                GlobalReferenceAxis.PositiveX, GlobalReferenceAxis.NegativeY, GlobalReferenceAxis.PositiveZ),
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
        internal CRSTransform(string name, GlobalReferenceAxis right, GlobalReferenceAxis up, GlobalReferenceAxis forward)
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

        internal CRSTransform(string name, Rotation reference)
        {
            Name = name;

            matrix3 = new Rotation(reference);
            globalAxes = new GlobalReferenceAxis[0];
        }

        internal CRSTransform(CRSTransform other)
        {
            Name = other.Name;
            Translation = other.Translation;

            matrix3 = new Rotation(other.matrix3);
            globalAxes = other.globalAxes;            
        }

        #endregion

        /// <summary>
        /// Meaningful name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Linear translation reference.
        /// </summary>
        public XYZ Translation { get; private set; } = new XYZ(0, 0, 0);

        /// <summary>
        /// Rotation due to reference CRS.
        /// </summary>
        public Rotation Rotation 
        {
            get => new Rotation(matrix3);
        }

        /// <summary>
        /// If using global predefined unit axes.
        /// </summary>
        public bool IsGlobal
        {
            get => globalAxes.Length == 3;
        }

        /// <summary>
        /// Clone transform having an offset to the existing translation part.
        /// </summary>
        /// <param name="theOffset">The addon offset.</param>
        /// <returns>New transform with same name</returns>
        public CRSTransform WithOffsetOf(XYZ theOffset)
        {
            return new CRSTransform(this) { Translation = Translation.Add(theOffset) };
        }

        /// <summary>
        /// Predefined unit axes if set.
        /// </summary>
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
