using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bitub.Dto.Concept;
using Dynamo.Graph.Nodes;
using Internal;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;

namespace Data
{
    /// <summary>
    /// Canonical filter node model with match type selector.
    /// </summary>
    [NodeName("Canonical Filter")]
    [NodeCategory("TRexIfc.Data")]
    [InPortTypes(new [] { nameof(Canonical) })]
    [OutPortTypes(nameof(CanonicalFilter))]
    [IsDesignScriptCompatible]
    public class CanonicalFilterNodeModel : BaseNodeModel 
    {
        #region Internals

        [JsonConstructor]
        CanonicalFilterNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        private FilterMatchingType filterMatchType = FilterMatchingType.Exists;

        #endregion

        /// <summary>
        /// A new model
        /// </summary>
        public CanonicalFilterNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("canonicals", "Canonical name oder ID")));
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("filter", "Canonical filter")));

            RegisterAllPorts();
        }

        /// <summary>
        /// The match type selector.
        /// </summary>
        public FilterMatchingType MatchType 
        {
            get {
                return filterMatchType;
            }
            set {
                filterMatchType = value;
                RaisePropertyChanged(nameof(MatchType));
                OnNodeModified(true);
            }
        }

#pragma warning disable CS1591

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            ClearErrorsAndWarnings();
            AssociativeNode[] inputs = inputAstNodes.ToArray();

            return new[]
            {
                AstFactory.BuildAssignment(
                    GetAstIdentifierForOutputIndex(0),
                    AstFactory.BuildFunctionCall(
                        new Func<object, Canonical[], CanonicalFilter>(CanonicalFilter.ByCanonicals),
                        new List<AssociativeNode>() {
                            AstFactory.BuildIntNode((int)MatchType),
                            inputs[0]
                        })
                    )
            };

        }

#pragma warning restore CS1591
    }
}
