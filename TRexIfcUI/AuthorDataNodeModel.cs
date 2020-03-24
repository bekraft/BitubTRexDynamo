using System;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Xbim.Ifc;

namespace TRexIfc
{
    /// <summary>
    /// Author's meta data. Will be embedded when changing / rewriting contents of IFC models.
    /// </summary>
    [NodeName("AuthorMetadata")]
    [NodeCategory("TRexIfc")]
    [OutPortTypes(typeof(IfcMetadata))]
    [OutPortDescriptions("Author's metadata")]
    [IsDesignScriptCompatible]
    public class AuthorDataNodeModel : NodeModel
    {
        #region Internals
        private string _organisationId = "";
        private string _organisationName = "";
        private string _organisationAddress = "";
        private string _authorName = "";
        private string _authorGivenName = "";        
        #endregion

        /// <summary>
        /// New authors data model.
        /// </summary>
        public AuthorDataNodeModel()
        {
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("authorData", "Author's metadata")));
            RegisterAllPorts();
        }

        [JsonConstructor]
        AuthorDataNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        /// <summary>
        /// The organisation name.
        /// </summary>
        public string OrganisationId
        {
            get => _organisationId;
            set {
                _organisationId = value;
                RaisePropertyChanged(nameof(OrganisationId));
                OnNodeModified();
            }
        }

        /// <summary>
        /// The organisation name.
        /// </summary>
        public string OrganisationName
        {
            get => _organisationName;
            set {
                _organisationName = value;
                RaisePropertyChanged(nameof(OrganisationName));
                OnNodeModified();
            }
        }

        /// <summary>
        /// The authors's name.
        /// </summary>
        public string AuthorName
        {
            get => _authorName;
            set {
                _authorName = value;
                RaisePropertyChanged(nameof(AuthorName));
                OnNodeModified();
            }
        }

        /// <summary>
        /// The author's given name.
        /// </summary>
        public string AuthorGivenName
        {
            get => _authorGivenName;
            set {
                _authorGivenName = value;
                RaisePropertyChanged(nameof(AuthorGivenName));
                OnNodeModified();
            }
        }

        /// <summary>
        /// The application name.
        /// </summary>
        public string OrganisationAddress
        {
            get => _organisationAddress;
            set {
                _organisationAddress = value;
                RaisePropertyChanged(nameof(OrganisationAddress));
                OnNodeModified();
            }
        }

        [IsVisibleInDynamoLibrary(false)]
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            var authorNode = AstFactory.BuildStringNode(_authorName);
            var authorGivenNode = AstFactory.BuildStringNode(_authorGivenName);
            var orgIdNode = AstFactory.BuildStringNode(_organisationId);
            var orgNameNode = AstFactory.BuildStringNode(_organisationName);
            var orgAddressNode = AstFactory.BuildStringNode(_organisationAddress);

            var funcNode = AstFactory.BuildFunctionCall(
                new Func<string, string, string, string, string, IfcMetadata>( IfcMetadata.ByAuthorAndOrganisation ),
                new List<AssociativeNode>() { authorNode, authorGivenNode, orgNameNode, orgIdNode, orgAddressNode });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), funcNode)
            };
        }
    }
}
