using System;
using System.Linq;

using Bitub.Dto;

using Autodesk.DesignScript.Runtime;

namespace TRex.Data
{
    /// <summary>
    /// A canonical name qualifier.
    /// </summary>
    public sealed class Canonical
    {
#pragma warning disable CS1591

        #region Internals

        internal Canonical(Qualifier q)
        {
            Qualifier = q;
        }

        #endregion

        internal Qualifier Qualifier { get; private set; }

        public override string ToString()
        {
            switch (Qualifier.GuidOrNameCase)
            {
                case Qualifier.GuidOrNameOneofCase.Anonymous:
                    return $"{Qualifier.Anonymous.GuidOrStringCase} ({Qualifier.ToLabel()})";
                default:
                    return $"{Qualifier.GuidOrNameCase} ({Qualifier.ToLabel()})";
            }

        }

#pragma warning restore CS1591

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
        public string ToLabel(string separator = ".") => Qualifier.ToLabel(separator);

        /// <summary>
        /// Returns the parts of the canonical name.
        /// </summary>
        /// <returns>An array of strings</returns>
        public string[] ToParts()
        {
            switch (Qualifier.GuidOrNameCase)
            {
                case Qualifier.GuidOrNameOneofCase.Anonymous:
                    return new[] { Qualifier.Anonymous.ToBase64String() };
                case Qualifier.GuidOrNameOneofCase.Named:
                    return Qualifier.Named.Frags.ToArray();
                case Qualifier.GuidOrNameOneofCase.None:
                    return new string[0];
                default:
                    throw new NotSupportedException($"Option '{Qualifier.GuidOrNameCase}' not supported");
            }
        }
    }
}
