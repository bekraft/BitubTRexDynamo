using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.DesignScript.Runtime;

using Bitub.Ifc;

namespace TRex.Store
{
    /// <summary>
    /// IFC authoring meta data
    /// </summary>
    [PreferredShortName("AuthorMetadata")]
    public class IfcAuthorMetadata
    {
        #region Internals

        internal IfcAuthorMetadata(IfcAuthoringMetadata authoringMetadata = null)
        {
            MetaData = authoringMetadata;
        }

        internal IfcAuthoringMetadata MetaData { get; set; }

        #endregion

        /// <summary>
        /// Extracts the <c>IfcMetadata</c> by repositories change history.
        /// </summary>
        /// <param name="ifcModel">The repository</param>
        /// <returns>A sorted list with most recent at top</returns>
        public static IfcAuthorMetadata[] OwnerHistory(IfcModel ifcModel)
        {
            if (null != ifcModel && !ifcModel.IsCanceled)
                return new IfcMetadataHistory(ifcModel.XbimModel).Chronically.Select(d => new IfcAuthorMetadata(d)).ToArray();
            else
                return new IfcAuthorMetadata[] { };
        }

        /// <summary>
        /// Create a new IFC project meta data by given credentials
        /// </summary>
        /// <param name="name">The owners name</param>
        /// <param name="givenName">The given name</param>
        /// <param name="organisationName">The authoring organisation name</param>
        /// <param name="organisationID">The authoring organisation ID</param>
        /// <param name="address">The address of the organisation</param>
        /// <returns>New meta data wrapping author's credentials and organisation details</returns>        
        public static IfcAuthorMetadata ByAuthorAndOrganisation(
            string name, 
            string givenName, 
            string organisationName, 
            string organisationID,
            string address)
        {
            OrganisationData org = new OrganisationData
            {
                Name = organisationName,
                Id = organisationID,
                Addresses = new AddressData[] 
                { 
                    new AddressData 
                    { 
                        Address = address, 
                        Type = Xbim.Ifc4.Interfaces.IfcAddressTypeEnum.OFFICE 
                    } 
                }
            };

            return new IfcAuthorMetadata
            {
                MetaData = new IfcAuthoringMetadata
                {                    
                    Editor = new AuthorData
                    {
                        Name = name,
                        GivenName = givenName,
                        Organisations = new OrganisationData[] { org }
                    }
                }
            };
        }

        /// <summary>
        /// Creates a new IFC metadate record by editor.
        /// </summary>
        /// <param name="newEditorName">A name</param>
        /// <param name="newGivenName">A given name</param>
        /// <returns>New metadata wrapping the author's credentials only</returns>
        public static IfcAuthorMetadata ByAuthor(string newEditorName, string newGivenName)
        {
            return new IfcAuthorMetadata
            {
                MetaData = new IfcAuthoringMetadata
                {
                    Editor = new AuthorData
                    {
                        Name = newEditorName,
                        GivenName = newGivenName
                    }
                }
            };
        }

        /// <summary>
        /// Turns the meta data into serialized represential data.
        /// </summary>
        /// <returns></returns>
        [MultiReturn("authorName", "authorGivenName", "ownerName", "ownerGivenName", "organisationName", "organisationID", "organisationAddress")]
        public Dictionary<string, object> ToData()
        {
            return new Dictionary<string, object>() 
            {                
                { "authorName", MetaData?.Editor?.Name },
                { "authorGivenName", MetaData?.Editor?.GivenName },
                { "ownerName", MetaData?.Owner?.Name },
                { "ownerGivenName", MetaData?.Owner?.GivenName },
                { "organisationName", MetaData?.Editor?.Organisations.Select(o => o.Name).ToArray() },
                { "organisationID", MetaData?.Editor?.Organisations.Select(o => o.Id).ToArray() },
                { "organisationAddress", MetaData?.Editor?.Organisations.Select(o => o.Addresses.Select(a => a.Address).ToArray()).ToArray() },
            };
        }
    }
}
