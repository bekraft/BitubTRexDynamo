using Microsoft.Extensions.Logging;

using System;
using System.Linq;

using Autodesk.DesignScript.Runtime;

using Internal;

using Bitub.Dto;

namespace Data
{
    /// <summary>
    /// A filtering node for canonical qualifiers.
    /// </summary>
    public class CanonicalFilter
    {
        #region Internals

        internal CanonicalFilter(Bitub.Dto.Concept.CanonicalFilter canonicalFilter)
        {
            filter = canonicalFilter;
        }

        #endregion

        /// <summary>
        /// Build a new filter by given matcher type and canoncials.
        /// </summary>
        /// <param name="matchingTypeEnum">The semantic wrapper type</param>
        /// <param name="canonicals">The canoncial</param>
        /// <returns>A new filter wrapper</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static CanonicalFilter ByCanonicals(object matchingTypeEnum, Canonical[] canonicals)
        {
            var matchingType = GlobalArgumentService.TryCastEnumOrDefault<Bitub.Dto.Concept.FilterMatchingType>(matchingTypeEnum);

            var filter = new Bitub.Dto.Concept.CanonicalFilter(matchingType, StringComparison.OrdinalIgnoreCase);
            filter.Filter.AddRange(canonicals.Select(c => c.qualifier.ToClassifier()));
            return new CanonicalFilter(filter); 
        }

        /// <summary>
        /// Builds a default filter accepting canonicals which are equivalent or sub canonicals.
        /// </summary>
        /// <param name="canonicals">The filtering canonical (super or equivalent)</param>
        /// <returns>A new filter</returns>
        public static CanonicalFilter ByCanonicals(params Canonical[] canonicals)
        {
            var filter = new Bitub.Dto.Concept.CanonicalFilter(Bitub.Dto.Concept.FilterMatchingType.SubOrEquiv, StringComparison.OrdinalIgnoreCase);
            filter.Filter.AddRange(canonicals.Select(c => c.qualifier.ToClassifier()));
            return new CanonicalFilter(filter);
        }

        /// <summary>
        /// Tests a given canonical against filter and returns matches of the filter.
        /// </summary>
        /// <param name="probe">The canoncial to test</param>
        /// <returns>Matches of the filter</returns>
        public Canonical[] FilterMatches(Canonical probe)
        {
            Classifier[] matches;
            filter.IsPassedBy(probe.qualifier, out matches);
            return matches.Select(c => new Canonical(c.Path.First())).ToArray();
        }

        /// <summary>
        /// Tests a given canonical against filter and returns matches of the the probes.
        /// </summary>
        /// <param name="probes">The canoncial to test</param>
        /// <returns>Matches of the filter</returns>
        public Canonical[] ProbeMatches(Canonical[] probes)
        {
            return probes.Where(p => filter.IsPassedBy(p.qualifier, out _) ?? true).ToArray();
        }

#pragma warning disable CS1591

        [IsVisibleInDynamoLibrary(false)]
        public readonly Bitub.Dto.Concept.CanonicalFilter filter;

        public override string ToString()
        {
            return $"[{filter.MatchingType}] filter with ({filter.Filter?.Count}) entries";
        }

#pragma warning restore CS1591

    }
}
