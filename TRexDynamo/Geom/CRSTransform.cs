using System.Linq;
using System.Collections.Generic;

using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;

using Newtonsoft.Json;

using Autodesk.DesignScript.Runtime;

using Google.Protobuf;

namespace TRex.Geom
{

    /// <summary>
    /// Coordinate reference transform which binds globally defined view directions and offset shift.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class CRSTransform
    {
        #region Internals
        
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
            Transform = new Transform(toCopy.Transform);
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
        /// Produces or sets the transform serialized data (JSON object).
        /// </summary>
        [JsonProperty("Transform")]
        [IsVisibleInDynamoLibrary(false)]
        public string Serialized
        {
            get => new JsonFormatter(JsonFormatter.Settings.Default.WithFormatDefaultValues(true)).Format(Transform);
            set => Transform = JsonParser.Default.Parse<Transform>(value);
        }
        
        #endregion
        
        
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
        public Transform Transform { get; set; } = Bitub.Dto.Scene.Transform.Identity;

        /// <summary>
        /// Returns the global accumulated transform. If no parent exists or identity, it will return
        /// the same as <see cref="Transform"/>.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public Transform GlobalTransform => ReverseTransforms()
            .Reverse()
            .Aggregate((t0, t1) => t0.Apply(t1));
        
        private IEnumerable<Transform> ReverseTransforms()
        {
            var crs = this;
            do
            {
                yield return crs.Transform;
            } 
            while (null != (crs = crs.parent));
        }

        /// <summary>
        /// New CRS as transform by given data.
        /// </summary>
        /// <param name="parent">The source transform</param>
        /// <param name="name">Name</param>
        /// <param name="serialized">The serialized data.</param>
        /// <returns>A transform</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static CRSTransform ByData(CRSTransform parent, string name, string serialized)
        {
            return new CRSTransform(parent, name)
            {
                Serialized = serialized
            }; 
        }
        

        /// <summary>
        /// Clones transform having an offset to the existing translation part.
        /// </summary>
        /// <param name="offset">The addon offset.</param>
        /// <param name="crs">The transform to be offset</param>
        /// <returns>New transform with same name</returns>
        public static CRSTransform ByOffsetTo(CRSTransform crs, XYZ offset)
        {
            if (null != offset)
            {
                return new CRSTransform(crs)
                {
                    Transform = 
                    {
                        T = crs.Transform.T.Add(offset)
                    }
                };
            }
            return crs;
        }

        /// <summary>
        /// Mirror X axis.
        /// </summary>
        /// <param name="sourceTransform">The source transform</param>
        /// <returns>New transform</returns>
        public static CRSTransform ByMirrorX(CRSTransform sourceTransform = null)
        {
            return new CRSTransform(sourceTransform, "Mirror X")
            {
                Transform = Transform.MirrorX
            };
        }

        /// <summary>
        /// Mirror Y axis.
        /// </summary>
        /// <param name="sourceTransform">The source transform</param>
        /// <returns>New transform</returns>
        public static CRSTransform ByMirrorY(CRSTransform sourceTransform = null)
        {
            return new CRSTransform(sourceTransform, "Mirror Y")
            {
                Transform = Transform.MirrorY
            };
        }
        
        /// <summary>
        /// Mirror Z axis.
        /// </summary>
        /// <param name="sourceTransform">The source transform</param>
        /// <returns>New transform</returns>
        public static CRSTransform ByMirrorZ(CRSTransform sourceTransform = null)
        {
            return new CRSTransform(sourceTransform, "Mirror Z")
            {
                Transform = Transform.MirrorZ
            };
        }

        /// <summary>
        /// RHS with Z-up based on top of another transformation.
        /// </summary>
        /// <returns>A standard CRS with Z-up right handed.</returns>
        public static CRSTransform ByRighthandZUp()
        {
            return new CRSTransform("RHS Z-Up")
            {
                Transform = Transform.RighthandZup
            };
        }

        /// <summary>
        /// RHS with Y-up based on top of another transformation.
        /// </summary>
        /// <returns>A standard CRS with Y-up right handed.</returns>
        public static CRSTransform ByRighthandYUp()
        {
            return new CRSTransform("RHS Y-Up")
            {
                Transform = Transform.RighthandYup
            };
        }

        /// <summary>
        /// LHS with Y-up based on top of another transformation.
        /// </summary>
        /// <returns>A CRS with Y-up left handed.</returns>
        public static CRSTransform ByLefthandYUp()
        {
            return new CRSTransform("LHS Y-Up")
            {
                Transform = Transform.LefthandYup
            };
        }

        /// <summary>
        /// LHS with Z-up based on top of another transformation.
        /// </summary>
        /// <returns>A CRS with Z-up left handed.</returns>
        public static CRSTransform ByLefthandZUp()
        {
            return new CRSTransform("LHS Z-Up")
            {
                Transform = Transform.LefthandZup
            };
        }

        /// <summary>
        /// Name representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Name ?? "Anonymous"}";
        }

        public override bool Equals(object obj)
        {
            return obj is CRSTransform transform &&
                   EqualityComparer<CRSTransform>.Default.Equals(parent, transform.parent) &&
                   Name == transform.Name &&
                   Transform == transform.Transform;
        }

        public override int GetHashCode()
        {
            var hashCode = -1324038637;
            hashCode = hashCode * -1521134295 + EqualityComparer<CRSTransform>.Default.GetHashCode(parent);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + Transform.GetHashCode();
            return hashCode;
        }
    }
}
