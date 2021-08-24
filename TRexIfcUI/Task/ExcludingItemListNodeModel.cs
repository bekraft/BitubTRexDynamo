using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Dynamo.Utilities;

using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Internal;

namespace TRex.Task
{
    /// <summary>
    /// Excludes multiple items from a list of available items using its string representation.
    /// </summary>
    [NodeName("Exclude items from")]
    [NodeCategory("TRex.Utils")]
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
            // Registration done by base
        }

#pragma warning disable CS1591

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            ClearErrorsAndWarnings();
            AssociativeNode resultList;
            if (IsPartiallyApplied)
            {
                resultList = AstFactory.BuildNullNode();
            }
            else
            {
                if (Selected.Any(v => v.IsTransient) || Selected.Count == 0)
                {   // Build from persistent selection
                    resultList = AstFactory.BuildFunctionCall(
                        new Func<object[], string[], bool, object[]>(DynamicArgumentDelegation.ExludeBySerializationValue),
                        new List<AssociativeNode>()
                        {
                            inputAstNodes[0],
                            AstFactory.BuildExprList(SelectedValue.Select(v => AstFactory.BuildStringNode(v) as AssociativeNode).ToList()),
                            AstFactory.BuildBooleanNode(false)
                        });
                }
                else
                {   // Build from live AST
                    resultList = AstFactory.BuildExprList(
                        Items.Where(c => !Selected.Contains(c)).Select(c => c.ToAstNode()).ToList());                    
                }
            }
            
            return new AssociativeNode[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), resultList)
            };
        }

#pragma warning restore CS1591
    }
}
