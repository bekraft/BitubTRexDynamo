using System;
using System.Linq;
using System.Collections.Generic;

using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Autodesk.DesignScript.Runtime;

using TRex.Export;
using System.Runtime.Serialization;

namespace TRex.Geom
{

    /// <summary>
    /// Coordinate reference transform which binds globally defined view directions and offset shift.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class CRSTransform
    {
        #region Internals

        private Transform transform = new Transform 
        { 
            R = Rotation.Identity, 
            T = XYZ.Zero
        };

        [JsonProperty("Right"), JsonConverter(typeof(StringEnumConverter))]
        private GlobalReferenceAxis right = GlobalReferenceAxis.PositiveX;
        [JsonProperty("Up"), JsonConverter(typeof(StringEnumConverter))]
        private GlobalReferenceAxis up = GlobalReferenceAxis.PositiveZ;
        [JsonProperty("Forward"), JsonConverter(typeof(StringEnumConverter))]
        private GlobalReferenceAxis forward = GlobalReferenceAxis.PositiveY;
        [JsonProperty("SourceCRS")]
        private CRSTransform parent = null;

        /// <summary>
        /// New default RHS with Z up.
        /// </summary>
        /// <param name="name"></param>
        private CRSTransform(string name)
        {            
            Name = name;
        }

        /// <summary>
        /// New clone from another CRS.
        /// </summary>
        /// <param name="toCopy"></param>
        private CRSTransform(CRSTransform toCopy)
        {
            Name = toCopy.Name;
            parent = toCopy.parent;

            right = toCopy.right;
            up = toCopy.up;
            forward = toCopy.forward;

            transform = new Transform(toCopy.transform);
        }

        /// <summary>
        /// New default CRS with a source parent CRS upstream.
        /// </summary>
        /// <param name="parent">Parent CRS</param>
        /// <param name="name">Name of new CRS</param>
        private CRSTransform(CRSTransform parent, string name)
        {
            Name = name;
            this.parent = parent;
        }

        /// <summary>
        /// Sets the mapping vector to transform.
        /// </summary>
        /// <param name="targetCRS">The target vector (this transform)</param>
        /// <param name="sourceCRS">The source vector (refererring transform)</param>
        private void SetReferenceVector(GlobalReferenceAxis targetCRS, GlobalReferenceAxis? sourceCRS)
        {
            switch (Math.Abs((int)targetCRS))
            {
                case 1:
                    transform.R.Rx = GetUnitAxis(sourceCRS ?? GlobalReferenceAxis.PositiveX).Scale(Math.Sign((int)targetCRS));
                    break;
                case 2:
                    transform.R.Ry = GetUnitAxis(sourceCRS ?? GlobalReferenceAxis.PositiveY).Scale(Math.Sign((int)targetCRS));
                    break;
                case 3:
                    transform.R.Rz = GetUnitAxis(sourceCRS ?? GlobalReferenceAxis.PositiveZ).Scale(Math.Sign((int)targetCRS));
                    break;
            }
        }

        #endregion

        [OnDeserialized, IsVisibleInDynamoLibrary(false)]
        public void OnDeserialized(StreamingContext sc)
        {
            SetReferenceVector(right, parent?.right);
            SetReferenceVector(up, parent?.up);
            SetReferenceVector(forward, parent?.forward);
        }

        /// <summary>
        /// Get the unit axis vector according to given global reference axis.
        /// </summary>
        /// <param name="referenceAxis">The global reference</param>
        /// <returns>A unit vector</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static XYZ GetUnitAxis(GlobalReferenceAxis referenceAxis)
        {
            int axis = (int)referenceAxis;
            switch (Math.Abs(axis))
            {
                case 1:
                    return new XYZ(Math.Sign(axis), 0, 0);
                case 2:
                    return new XYZ(0, Math.Sign(axis), 0);
                case 3:
                    return new XYZ(0, 0, Math.Sign(axis));
            }
            throw new NotSupportedException();
        }

        /// <summary>
        /// New RHS CRS with Z-axis pointing upwards.
        /// </summary>
        [JsonConstructor]
        [IsVisibleInDynamoLibrary(false)]
        public CRSTransform()
        { }

        /// <summary>
        /// Name of CRS.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        [JsonProperty]
        public string Name { get; set; }

        /// <summary>
        /// Local transform of CRS.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public Transform Transform
        {
            get => new Transform(transform);
        }

        /// <summary>
        /// Expands all parent CRSs left hand.
        /// </summary>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public IEnumerable<CRSTransform> ExpandLeft()
        {
            var crs = this;
            do
            {
                yield return crs;
            } 
            while (null != (crs = crs.parent));
        }

        /// <summary>
        /// Whether the CRS is valid (when having 3 distinct reference axis).
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public bool IsGloballyValid
        {
            get => (new[] { Right, Up, Forward }.Distinct().Count() == 3);
        }

        /// <summary>
        /// Right reference axis.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public GlobalReferenceAxis Right
        {
            get
            {
                return right;
            }
            private set
            {
                right = value;
                SetReferenceVector(right, parent?.right);
            }
        }

        /// <summary>
        /// Up reference axis.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public GlobalReferenceAxis Up
        {
            get
            {
                return up;
            }
            private set
            {
                up = value;
                SetReferenceVector(up, parent?.up);
            }
        }

        /// <summary>
        /// Forward reference axis.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public GlobalReferenceAxis Forward
        {
            get
            {
                return forward;
            }
            private set
            {
                forward = value;
                SetReferenceVector(forward, parent?.forward);
            }
        }

        /// <summary>
        /// New CRS as transform based on default RHS Z-Up source transform.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="right">Right orientation</param>
        /// <param name="up">Up orientation</param>
        /// <param name="forward">Forward orientation</param>
        /// <returns>A transform</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static CRSTransform ByData(string name, CRSTransform source, string right, string up, string forward)
        {
            return new CRSTransform(source, name)
            {
                Right = (GlobalReferenceAxis)Enum.Parse(typeof(GlobalReferenceAxis), right),
                Up = (GlobalReferenceAxis)Enum.Parse(typeof(GlobalReferenceAxis), up),
                Forward = (GlobalReferenceAxis)Enum.Parse(typeof(GlobalReferenceAxis), forward)
            }; 
        }

        [IsVisibleInDynamoLibrary(false)]
        public static CRSTransform ByData(string name, CRSTransform source, GlobalReferenceAxis right, GlobalReferenceAxis up, GlobalReferenceAxis forward)
        {
            return new CRSTransform(source, name)
            {
                Right = right,
                Up = up,
                Forward = forward
            };
        }

        /// <summary>
        /// Clones transform having an offset to the existing translation part.
        /// </summary>
        /// <param name="offset">The addon offset.</param>
        /// <param name="crs">The transform to be offseted</param>
        /// <returns>New transform with same name</returns>
        public static CRSTransform ByOffsetTo(CRSTransform crs, XYZ offset)
        {
            CRSTransform newCrs = null;
            if (null != offset)
            {
                newCrs = new CRSTransform(crs);
                newCrs.transform.T = crs.transform.T.Add(offset);
            }
            else
            {
                newCrs = crs;
            }
            return crs;
        }

        /// <summary>
        /// Mirror right axis.
        /// </summary>
        /// <param name="sourceTransform">The source transform</param>
        /// <returns>New transform</returns>
        public static CRSTransform ByMirrorX(CRSTransform sourceTransform)
        {
            return new CRSTransform(sourceTransform, "Mirror Right")
            {
                Right = sourceTransform.Right.Invert(),
                Up = sourceTransform.Up,
                Forward = sourceTransform.Forward

            };
        }

        /// <summary>
        /// Default RHS coordinate reference.
        /// </summary>
        /// <returns>A standard CRS with Z-up right handed.</returns>        
        public static CRSTransform ByRighthandZUp()
        {
            return new CRSTransform("RHS Z-Up");
        }

        /// <summary>
        /// RHS with Z-up based on top of another transformation.
        /// </summary>
        /// <param name="sourceTransform">The source</param>
        /// <returns>A standard CRS with Z-up right handed.</returns>
        public static CRSTransform ByRighthandZUp(CRSTransform sourceTransform)
        {
            return new CRSTransform(sourceTransform, "RHS Z-Up")
            {
                Right = GlobalReferenceAxis.PositiveX,
                Up = GlobalReferenceAxis.PositiveZ,
                Forward = GlobalReferenceAxis.PositiveY
            };
        }

        /// <summary>
        /// RHS with Y-up based on top of another transformation.
        /// </summary>
        /// <param name="sourceTransform">The source</param>
        /// <returns>A standard CRS with Y-up right handed.</returns>
        public static CRSTransform ByRighthandYUp(CRSTransform sourceTransform)
        {
            return new CRSTransform(sourceTransform, "RHS Y-Up")
            {
                Right = GlobalReferenceAxis.PositiveX,
                Up = GlobalReferenceAxis.PositiveY,
                Forward = GlobalReferenceAxis.NegativeZ
            };
        }

        /// <summary>
        /// LHS with Y-up based on top of another transformation.
        /// </summary>
        /// <param name="sourceTransform">The source</param>
        /// <returns>A CRS with Y-up left handed.</returns>
        public static CRSTransform ByLefthandYUp(CRSTransform sourceTransform)
        {
            return new CRSTransform(sourceTransform, "LHS Y-Up")
            {
                Right = GlobalReferenceAxis.PositiveX,
                Up = GlobalReferenceAxis.PositiveY,
                Forward = GlobalReferenceAxis.PositiveZ
            };
        }

        /// <summary>
        /// LHS with Z-up based on top of another transformation.
        /// </summary>
        /// <param name="sourceTransform">The source</param>
        /// <returns>A CRS with Z-up left handed.</returns>
        public static CRSTransform ByLefthandZUp(CRSTransform sourceTransform)
        {
            return new CRSTransform("LHS Z-Up")
            {
                Right = GlobalReferenceAxis.PositiveX,
                Up = GlobalReferenceAxis.PositiveZ,
                Forward = GlobalReferenceAxis.NegativeY
            };
        }

        public override string ToString()
        {
            var axes = new[] { Right, Up, Forward }.Distinct();
            return $"{Name ?? "Anonymous"}({string.Join("|", axes.Select(a => a.ToString()))})";
        }

        public override bool Equals(object obj)
        {
            return obj is CRSTransform transform &&
                   EqualityComparer<CRSTransform>.Default.Equals(parent, transform.parent) &&
                   Name == transform.Name &&
                   Right == transform.Right &&
                   Up == transform.Up &&
                   Forward == transform.Forward;
        }

        public override int GetHashCode()
        {
            int hashCode = -1324038637;
            hashCode = hashCode * -1521134295 + EqualityComparer<CRSTransform>.Default.GetHashCode(parent);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + Right.GetHashCode();
            hashCode = hashCode * -1521134295 + Up.GetHashCode();
            hashCode = hashCode * -1521134295 + Forward.GetHashCode();
            return hashCode;
        }
    }
}
