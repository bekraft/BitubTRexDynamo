using System;
using System.Linq;

using Bitub.Dto;

using Autodesk.DesignScript.Runtime;

namespace Data
{
    /// <summary>
    /// A canonical name qualifier.
    /// </summary>
    public class Canonical
    {
        #region Internals

        internal Canonical(Qualifier q)
        {
            qualifier = q;
        }

        #endregion

        /// <summary>
        /// A new canonical by given candidate GUID.
        /// </summary>
        /// <param name="guidOrBase64">Either string representation or base64 generic representation</param>
        /// <returns>An anonymous canonical</returns>
        public static Canonical ByGuid(string guidOrBase64)
        {
            return new Canonical(new Qualifier { Anonymous = guidOrBase64.ToGlobalUniqueId() });
        }

        /// <summary>
        /// New canoncial by given sequence of named parts.
        /// </summary>
        /// <param name="namePart">A partial name</param>
        /// <returns>The new named canoncial</returns>
        public static Canonical ByNames(params string[] namePart)
        {
            var name = new Name();
            name.Frags.AddRange(namePart);
            return new Canonical(new Qualifier { Named = name });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="separator"></param>
        /// <returns>A string</returns>
        public string ToLabel(string separator = ".") => qualifier.ToLabel(separator);

        /// <summary>
        /// Returns the parts of the canonical name.
        /// </summary>
        /// <returns>An array of strings</returns>
        public string[] ToParts()
        {
            switch (qualifier.GuidOrNameCase)
            {
                case Qualifier.GuidOrNameOneofCase.Anonymous:
                    return new[] { qualifier.Anonymous.ToBase64String() };
                case Qualifier.GuidOrNameOneofCase.Named:
                    return qualifier.Named.Frags.ToArray();
                case Qualifier.GuidOrNameOneofCase.None:
                    return new string[0];
                default:
                    throw new NotSupportedException($"Option '{qualifier.GuidOrNameCase}' not supported");
            }
        }

#pragma warning disable CS1591

        [IsVisibleInDynamoLibrary(false)]
        public readonly Qualifier qualifier;

        public override string ToString()
        {
            switch (qualifier.GuidOrNameCase)
            {
                case Qualifier.GuidOrNameOneofCase.Anonymous:
                    return $"{qualifier.Anonymous.GuidOrStringCase} ({qualifier.ToLabel()})";
                default:
                    return $"{qualifier.GuidOrNameCase} ({qualifier.ToLabel()})";
            }
            
        }

#pragma warning restore CS1591
    }
}
