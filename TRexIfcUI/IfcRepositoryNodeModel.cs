using System;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Xbim.Ifc;

namespace TRexIfc
{
    [NodeName("IFC Repository")]
    [NodeCategory("TRexIfc")]
    [OutPortNames("ifcRepository")]
    [OutPortTypes(typeof(IfcRepository))]
    [OutPortDescriptions("IFC repository")]
    [IsDesignScriptCompatible]
    public class IfcRepositoryNodeModel : NodeModel
    {
        #region Internals
        private IfcRepository _ifcRepository;

        #endregion

        public IfcRepositoryNodeModel()
        {
            RegisterAllPorts();
        }

        [JsonConstructor]
        IfcRepositoryNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        [IsVisibleInDynamoLibrary(false)]
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {

        }
    }
}
