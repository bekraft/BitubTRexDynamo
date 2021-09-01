using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bitub.Dto;
using Bitub.Dto.Concept;

using Autodesk.DesignScript.Runtime;

namespace TRex.Data
{
    /// <summary>
    /// A property group binds a element qualifier filter to a group of properties.
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
        protected CanonicalFilter elementTypeFilter;

        [IsVisibleInDynamoLibrary(false)]
        public string Name { get; set; }

        [IsVisibleInDynamoLibrary(false)]
        public virtual CanonicalFilter ElementFilter
        {
            get => elementTypeFilter;
            set => elementTypeFilter = value;
        }

        [IsVisibleInDynamoLibrary(false)]
        public virtual bool AddProperty(ELFeature featureConcept)
        {
            if (properties.ContainsKey(featureConcept.Name))
                return false;

            properties.Add(featureConcept.Name, new Property (featureConcept));
            return true;
        }

        [IsVisibleInDynamoLibrary(false)]
        public virtual bool RemoveProperty(ELFeature featureConcept)
        {
            return properties.Remove(featureConcept.Name);
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
