using Bitub.Ifc.Transform.Requests;

namespace Task
{
    /// <summary>
    /// Property set removal preferences.
    /// </summary>
    public class PSetRemovalRequest
    {
        #region Internals

        internal IfcPropertySetRemovalRequest Request { get; set; }

        internal PSetRemovalRequest()
        { }

        #endregion

        /// <summary>
        /// New removal preferences by property set names. Will always be case insensitive matching
        /// </summary>
        /// <param name="propertySetNames">The property names (any case)</param>
        /// <returns>New preferences</returns>
        public PSetRemovalRequest ByPropertySets(params string[] propertySetNames)
        {
            return new PSetRemovalRequest
            {
                Request = new IfcPropertySetRemovalRequest
                {
                    BlackListNames = propertySetNames,
                    IsNameMatchingCaseSensitive = false
                }
            };
        }

        /// <summary>
        /// New removal preferences by proeperty set names.
        /// </summary>
        /// <param name="propertySetNames">The names (case sensitive depending on <c>caseSensitiveMatching</c></param>
        /// <param name="caseSensitiveMatching">Whether to match exactly by case or whether to ignore case</param>
        /// <returns>New preferences</returns>
        public PSetRemovalRequest ByPropertySets(string[] propertySetNames, bool caseSensitiveMatching)
        {
            return new PSetRemovalRequest
            {
                Request = new IfcPropertySetRemovalRequest
                {
                    BlackListNames = propertySetNames,
                    IsNameMatchingCaseSensitive = caseSensitiveMatching
                }
            };
        }
    }
}
