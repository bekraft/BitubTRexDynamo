using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Dynamo.Utilities;

using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;
using System;
using Internal;

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
    public class SelectingItemListNodeModel : SelectableItemListNodeModel
    {
        #region Internals

        [JsonConstructor]
        SelectingItemListNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        #endregion

        /// <summary>
        /// New selective item list node model.
        /// </summary>
        public SelectingItemListNodeModel() : base()
        {
        }

#pragma warning disable CS1591

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            ClearErrorsAndWarnings();
            if (IsPartiallyApplied)
            {
                return new AssociativeNode[]
                {
                    AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                };
            }

            AssociativeNode selectedNode;
            if (Selected?.Count > 0)
            {
                selectedNode = AstFactory.BuildExprList(Selected.Select(c => c.ToAstNode()).ToList());
            }
            else
            {
                selectedNode = AstFactory.BuildFunctionCall(
                    new Func<object[], string[], bool, object[]>(GlobalArgumentService.FilterBySerializationValue),
                    new List<AssociativeNode>()
                    {
                         inputAstNodes[0],
                         AstFactory.BuildExprList(SelectedValue.Select(v => AstFactory.BuildStringNode(v) as AssociativeNode).ToList()),
                         AstFactory.BuildBooleanNode(false)
                    });
            }
            
            return new AssociativeNode[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), selectedNode)
            };
        }

#pragma warning restore CS1591
    }
}
