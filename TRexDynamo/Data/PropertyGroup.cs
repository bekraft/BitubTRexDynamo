using System;
using System.Collections;
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
    /// A property group.
    /// </summary>
    public class PropertyGroup : IEnumerable<Property>
    {
        #region Internals        

        internal PropertyGroup(QualifierCaseEqualityComparer caseEqualityComparer)
        {
            properties = new Dictionary<Qualifier, Property>(caseEqualityComparer);
        }

#pragma warning disable CS1591

        protected IDictionary<Qualifier, Property> properties;
        protected CanonicalFilter elementTypeFiler;

        [IsVisibleInDynamoLibrary(false)]
        public string Name { get; set; }

        [IsVisibleInDynamoLibrary(false)]
        public virtual CanonicalFilter ElementFilter
        {
            get => elementTypeFiler;
            set => elementTypeFiler = value;
        }

        [IsVisibleInDynamoLibrary(false)]
        public virtual bool AddProperty(FeatureConcept featureConcept)
        {
            if (properties.ContainsKey(featureConcept.Canonical))
                return false;

            properties.Add(featureConcept.Canonical, new Property (featureConcept));
            return true;
        }

        [IsVisibleInDynamoLibrary(false)]
        public virtual bool RemoveProperty(FeatureConcept featureConcept)
        {
            return properties.Remove(featureConcept.Canonical);
        }

        [IsVisibleInDynamoLibrary(false)]
        public IEnumerator<Property> GetEnumerator() => properties.Values.GetEnumerator();

        [IsVisibleInDynamoLibrary(false)]
        IEnumerator IEnumerable.GetEnumerator() => properties.Values.GetEnumerator();

#pragma warning restore CS1591

        #endregion
        /*
        public static PropertyGroup ByProperty(string propertyGroup, string[] names, object[] value = null)
        {            
            var group = new PropertyGroup(new QualifierCaseEqualityComparer(StringComparison.OrdinalIgnoreCase));
            for (int i=0; i<names.Length; i++)
            {
                object v = null;
                if (value?.Length > i)
                    v = value[i];

                group.AddProperty()
            }
        }*/
    }
}
