using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.DesignScript.Runtime;

using Bitub.Ifc;

namespace TRexIfc
{
    /// <summary>
    /// IFC Meta data
    /// </summary>
    public class IfcMetadata
    {
        #region Internals

        internal IfcMetadata()
        {
        }

        internal IfcProjectMetadata MetaData { get; set; }

        #endregion

        /// <summary>
        /// Extracts the <c>IfcMetadata</c> by repositories change history.
        /// </summary>
        /// <param name="ifcRepository">The repository</param>
        /// <returns>A sorted list with most recent at top</returns>
        public static IfcMetadata[] ByIfcHistory(IfcRepository ifcRepository)
        {
            return new IfcMetadata[0];
        }

        /// <summary>
        /// Create a new IFC project meta data by given credentials
        /// </summary>
        /// <param name="name">The owners name</param>
        /// <param name="givenName">The given name</param>
        /// <param name="applicationName">The authoring application name</param>
        /// <param name="applicationID">The authoring application ID</param>
        /// <returns>New meta data</returns>
        public static IfcMetadata ByEditorAndApplication(
            string name, 
            string givenName, 
            string applicationName, 
            string applicationID)
        {
            return new IfcMetadata
            {
                MetaData = new IfcProjectMetadata
                {
                    AuthoringApplication = new ApplicationData
                    {
                        ApplicationID = applicationID,
                        ApplicationName = applicationName
                    },
                    Owner = new AuthorData
                    {
                        Name = name,
                        GivenName = givenName
                    }
                }
            };
        }

        /// <summary>
        /// Creates a new IFC metadate record by editor.
        /// </summary>
        /// <param name="newEditorName">A name</param>
        /// <param name="newGivenName">A given name</param>
        /// <returns>A new IfcMetadata</returns>
        public static IfcMetadata ByNewEditor(string newEditorName, string newGivenName)
        {
            return new IfcMetadata
            {
                MetaData = new IfcProjectMetadata
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
        /// Changes the editor name.
        /// </summary>
        /// <param name="newEditorName">A new name</param>
        /// <param name="newGivenName">A new given name</param>
        /// <returns>Changed meta data</returns>
        public IfcMetadata NewEditorName(string newEditorName, string newGivenName)
        {
            MetaData.Editor = new AuthorData
            {
                Name = newEditorName,
                GivenName = newGivenName
            };
            return this;
        }

        /// <summary>
        /// Changes the application credentials.
        /// </summary>
        /// <param name="newApplicationName">A new application name</param>
        /// <param name="newApplicationID">A new application ID</param>
        /// <returns>Changed meta data</returns>
        public IfcMetadata NewApplication(string newApplicationName, string newApplicationID)
        {
            MetaData.AuthoringApplication = new ApplicationData
            {
                ApplicationName = newApplicationName,
                ApplicationID = newApplicationID
            };
            return this;
        }

    }
}
