using System;
using System.Linq;
using System.Collections.Generic;

using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;

using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Autodesk.DesignScript.Runtime;

using TRex.Internal;

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

        UserDefined = 0,

        PositiveX = 1,
        PositiveY = 2,
        PositiveZ = 3
    }

    /// <summary>
    /// Coordinate reference transform which binds globally defined view directions and offset shift.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class CRSTransform
    {
        #region Internals

        private Transform global = new Transform 
        { 
            R = Rotation.Identity, 
            T = XYZ.Zero
        };

        [JsonProperty("Right"), JsonConverter(typeof(StringEnumConverter))]
        private GlobalReferenceAxis right = GlobalReferenceAxis.UserDefined;
        [JsonProperty("Up"), JsonConverter(typeof(StringEnumConverter))]
        private GlobalReferenceAxis up = GlobalReferenceAxis.UserDefined;
        [JsonProperty("Forward"), JsonConverter(typeof(StringEnumConverter))]
        private GlobalReferenceAxis forward = GlobalReferenceAxis.UserDefined;

        internal CRSTransform(string name, GlobalReferenceAxis right, GlobalReferenceAxis up, GlobalReferenceAxis forward)
        {            
            Right = right;
            Up = up;
            Forward = forward;

            Name = name;

            CompileTransform();

            if (!IsGloballyValid)
                throw new ArgumentException("Transform is not valid by given reference axis.");
        }

        internal CRSTransform(Transform t)
        {
            this.global = new Transform(t);
        }

        internal CRSTransform(string name, Rotation reference)
        {
            Name = name;
            this.global.R = new Rotation(reference);
        }

        internal CRSTransform(CRSTransform other)
        {
            Name = other.Name;

            this.global = new Transform(other.global);

            Right = other.Right;
            Up = other.Up;
            Forward = other.Forward;
        }

        internal static CRSTransform ByDynamic(dynamic crs)
        {
            try
            {
                string name = crs.Name;
                GlobalReferenceAxis right = crs.Right;
                GlobalReferenceAxis up = crs.Up;
                GlobalReferenceAxis forward = crs.Forward;
                return new CRSTransform(name, right, up, forward);
            }
            catch (Exception e)
            {
                GlobalLogging.log.Warning("Unable to parse '{0}' as format: {1}", crs, e.Message);
            }
            return null;
        }

        internal XYZ GetUnitAxis(GlobalReferenceAxis referenceAxis)
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

        #endregion

        [JsonConstructor]
        [IsVisibleInDynamoLibrary(false)]
        public CRSTransform()
        { }

        private void CompileTransform()
        {
            this.global.R = new Rotation
            {
                Rx = GetUnitAxis(right),
                Ry = GetUnitAxis(forward),
                Rz = GetUnitAxis(up),
            };
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext sc)
        {
            CompileTransform();
        }

        /// <summary>
        /// Meaningful name.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        [JsonProperty]
        public string Name { get; set; }

        /// <summary>
        /// The full transform
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public Transform Transform
        {
            get => new Transform(global);
        }

        [IsVisibleInDynamoLibrary(false)]
        public bool IsGloballyValid
        {
            get => IsGlobal && (new[] { Right, Up, Forward }.Distinct().Count() == 3);
        }

        [IsVisibleInDynamoLibrary(false)]
        public bool IsGlobal
        {
            get => new[] { Right, Up, Forward }.All(a => a != GlobalReferenceAxis.UserDefined);
        }

        [IsVisibleInDynamoLibrary(false)]
        public GlobalReferenceAxis Right
        {
            get => right;
            private set => right = value;
        }

        [IsVisibleInDynamoLibrary(false)]
        public GlobalReferenceAxis Up
        {
            get => up;
            private set => up = value;
        }

        [IsVisibleInDynamoLibrary(false)]
        public GlobalReferenceAxis Forward
        {
            get => forward;
            private set => forward = value;
        }

        [IsVisibleInDynamoLibrary(false)]
        public static CRSTransform ByData(string name, string right, string up, string forward)
        {
            return new CRSTransform(
                name,
                (GlobalReferenceAxis)Enum.Parse(typeof(GlobalReferenceAxis), right),
                (GlobalReferenceAxis)Enum.Parse(typeof(GlobalReferenceAxis), up),
                (GlobalReferenceAxis)Enum.Parse(typeof(GlobalReferenceAxis), forward)
            ); 
        }

        /// <summary>
        /// Clones transform having an offset to the existing translation part.
        /// </summary>
        /// <param name="theOffset">The addon offset.</param>
        /// <param name="transform">The transform to be offseted</param>
        /// <returns>New transform with same name</returns>
        public static CRSTransform ByOffsetTo(CRSTransform transform, XYZ theOffset)
        {
            CRSTransform crs = null;
            if (null != theOffset)
            {
                crs = new CRSTransform(transform);
                crs.global.T = transform.global.T.Add(theOffset);
            }
            else
            {
                crs = transform;
            }
            return crs;
        }

        public static CRSTransform ByRighthandZUp(string name = null)
        {
            return new CRSTransform(name ?? "RHS Z-Up", GlobalReferenceAxis.PositiveX, GlobalReferenceAxis.PositiveZ, GlobalReferenceAxis.PositiveY);
        }

        public static CRSTransform ByRighthandYUp(string name = null)
        {
            return new CRSTransform(name ?? "RHS Y-Up", GlobalReferenceAxis.PositiveX, GlobalReferenceAxis.PositiveY, GlobalReferenceAxis.NegativeZ);
        }

        public static CRSTransform ByLefthandYUp(string name = null)
        {
            return new CRSTransform(name ?? "LHS Y-Up", GlobalReferenceAxis.PositiveX, GlobalReferenceAxis.PositiveY, GlobalReferenceAxis.PositiveZ);
        }

        public static CRSTransform ByLefthandZUp(string name = null)
        {
            return new CRSTransform(name ?? "LHS Z-Up", GlobalReferenceAxis.PositiveX, GlobalReferenceAxis.PositiveZ, GlobalReferenceAxis.NegativeY);
        }

        public override string ToString()
        {
            var axes = new[] { Right, Up, Forward }.Distinct();
            return $"{Name ?? "Anonymous"}({string.Join("|", axes.Select(a => a.ToString()))})";
        }

        public override bool Equals(object obj)
        {
            return obj is CRSTransform transform &&
                   EqualityComparer<Transform>.Default.Equals(global, transform.global) &&
                   Name == transform.Name &&
                   Right == transform.Right &&
                   Up == transform.Up &&
                   Forward == transform.Forward;
        }

        public override int GetHashCode()
        {
            int hashCode = -1324038637;
            hashCode = hashCode * -1521134295 + EqualityComparer<Transform>.Default.GetHashCode(global);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + Right.GetHashCode();
            hashCode = hashCode * -1521134295 + Up.GetHashCode();
            hashCode = hashCode * -1521134295 + Forward.GetHashCode();
            return hashCode;
        }
    }
}
