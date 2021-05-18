using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bitub.Dto;
using Bitub.Dto.Concept;

using Autodesk.DesignScript.Runtime;

namespace Data
{
    /// <summary>
    /// Model property wrapper.
    /// </summary>
    public sealed class Property
    {
#pragma warning disable CS1591

        #region Internals

        internal Property(FeatureConcept featureConcept)
        {
            Feature = featureConcept;
        }

        internal FeatureConcept Feature { get; private set; }

        public override string ToString()
        {
            return $"{Feature.Canonical.ToLabel()}";
        }

        internal static Property ByData(Canonical canonical, DataType dataType, object value)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return Feature?.Equals(obj) ?? this == obj;
        }

        public override int GetHashCode()
        {
            return Feature?.GetHashCode() ?? GetHashCode();
        }

#pragma warning restore CS1591

        public string QualifiedName
        {
            get => Feature.Canonical.ToLabel(":");
        }

        public string PropertySetName
        {
            get => Feature.Canonical.Named.GetFragment(0);
            internal set => Feature.Canonical.Named.SetFragment(0, value);
        }

        public string PropertyName
        {
            get => Feature.Canonical.Named.GetFragment(1);
            internal set => Feature.Canonical.Named.SetFragment(1, value);
        }

        #endregion

        /// <summary>
        /// Returns an array of property name, type and values.
        /// </summary>
        /// <returns></returns>
        public object[] ToData()
        {
            throw new NotImplementedException();
        }
    }
}
