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
    public class Property
    {
        #region Internals

#pragma warning disable CS1591

        [IsVisibleInDynamoLibrary(false)]
        public Property(FeatureConcept featureConcept)
        {
            Feature = featureConcept;
        }

        [IsVisibleInDynamoLibrary(false)]
        public FeatureConcept Feature { get; private set; }

        public override string ToString()
        {
            return $"{Feature.Canonical.ToLabel()}";
        }

        [IsVisibleInDynamoLibrary(false)]
        public static Property ByData(Canonical canonical, DataType dataType, object value)
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

        [IsVisibleInDynamoLibrary(false)]
        public string QualifiedName
        {
            get => Feature.Canonical.ToLabel(":");
        }

        [IsVisibleInDynamoLibrary(false)]
        public string PropertySetName
        {
            get => Feature.Canonical.Named.GetFragment(0);
            set => Feature.Canonical.Named.SetFragment(0, value);
        }

        [IsVisibleInDynamoLibrary(false)]
        public string PropertyName
        {
            get => Feature.Canonical.Named.GetFragment(1);
            set => Feature.Canonical.Named.SetFragment(1, value);
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
