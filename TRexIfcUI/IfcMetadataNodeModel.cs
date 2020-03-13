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
    /// IFC model metadata properties.
    /// </summary>
    [NodeName("IFC Metadata")]
    [NodeCategory("TRexIfc")]
    [OutPortNames("ifcMetaData")]
    [OutPortTypes(typeof(IfcMetadata))]
    [OutPortDescriptions("IFC meta data")]
    [IsDesignScriptCompatible]
    public class IfcMetadataNodeModel : NodeModel
    {
        #region Internals
        private string _applicationName = "";
        private string _applicationID = "";
        private string _editorName = "";
        private string _editorGivenName = "";
        #endregion

        public IfcMetadataNodeModel()
        {
            //OutPorts.Add(new PortModel(PortType.Output, this, new PortData("IFC metadata", "Metadata")));
            RegisterAllPorts();
        }

        [JsonConstructor]
        IfcMetadataNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        /// <summary>
        /// The application name.
        /// </summary>
        public string ApplicationName
        {
            get => _applicationName;
            set {
                _applicationName = value;
                RaisePropertyChanged(nameof(ApplicationName));
                OnNodeModified();
            }
        }

        /// <summary>
        /// The editor's name.
        /// </summary>
        public string EditorName
        {
            get => _editorName;
            set {
                _editorName = value;
                RaisePropertyChanged(nameof(EditorName));
                OnNodeModified();
            }
        }

        /// <summary>
        /// The editor's given name.
        /// </summary>
        public string EditorGivenName
        {
            get => _editorGivenName;
            set {
                _editorGivenName = value;
                RaisePropertyChanged(nameof(EditorGivenName));
                OnNodeModified();
            }
        }

        /// <summary>
        /// The application name.
        /// </summary>
        public string ApplicationID
        {
            get => _applicationID;
            set {
                _applicationID = value;
                RaisePropertyChanged(nameof(ApplicationID));
                OnNodeModified();
            }
        }

        [IsVisibleInDynamoLibrary(false)]
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            var editorNode = AstFactory.BuildStringNode(_editorName);
            var editorGivenNode = AstFactory.BuildStringNode(_editorGivenName);
            var applicationIdNode = AstFactory.BuildStringNode(_applicationID);
            var applicationNode = AstFactory.BuildStringNode(_applicationName);

            var funcNode = AstFactory.BuildFunctionCall(
                new Func<string, string, string, string, IfcMetadata>( IfcMetadata.ByEditorAndApplication ),
                new List<AssociativeNode>() { editorNode, editorGivenNode, applicationNode, applicationIdNode });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), funcNode)
            };
        }
    }
}
