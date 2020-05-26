using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Dynamo.Utilities;

using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

namespace Task
{
    /// <summary>
    /// Select multiple items from a list of available items using its string representation.
    /// </summary>
    [NodeName("Select items from")]
    [NodeCategory("TRexIfc.Task.Tasks")]
    [NodeDescription("Select multiple items from a list of available items using its string representation.")]
    [InPortTypes(typeof(object[]))]
    [OutPortTypes(typeof(object[]))]
    [IsDesignScriptCompatible]
    public class SelectiveItemListNodeModel : SelectableItemListNodeModel
    {
        #region Internals

        [JsonConstructor]
        SelectiveItemListNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        #endregion

        /// <summary>
        /// New selective item list node model.
        /// </summary>
        public SelectiveItemListNodeModel() : base()
        {
        }

#pragma warning disable CS1591

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            if (IsPartiallyApplied)
            {
                return new AssociativeNode[]
                {
                    AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                };
            }

            AssociativeNode selectedNode;
            if (Selected?.Count > 0)
                selectedNode = AstFactory.BuildExprList(
                        Selected.Select(c => c.ToAstNode() as AssociativeNode).ToList());
            else
                selectedNode = AstFactory.BuildNullNode();

            return new AssociativeNode[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), selectedNode)
            };
        }

#pragma warning restore CS1591
    }
}
