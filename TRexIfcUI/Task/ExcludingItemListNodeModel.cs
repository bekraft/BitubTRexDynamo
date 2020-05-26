using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Dynamo.Utilities;

using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

namespace Task
{
    /// <summary>
    /// Excludes multiple items from a list of available items using its string representation.
    /// </summary>
    [NodeName("Exclude items from")]
    [NodeCategory("TRexIfc.Task.Tasks")]
    [NodeDescription("Excludes multiple items from a list of available items using its string representation.")]
    [InPortTypes(typeof(object[]))]
    [OutPortTypes(typeof(object[]))]
    [IsDesignScriptCompatible]
    public class ExcludingItemListNodeModel : SelectableItemListNodeModel
    {
        #region Internals

        [JsonConstructor]
        ExcludingItemListNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        #endregion

        /// <summary>
        /// New excluding item list node model.
        /// </summary>
        public ExcludingItemListNodeModel() : base()
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
                        Items.Where(c => !Selected.Contains(c)).Select(c => c.ToAstNode()).ToList());
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
