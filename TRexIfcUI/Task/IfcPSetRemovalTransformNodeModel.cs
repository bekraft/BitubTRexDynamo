using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Internal;
using Task;
using Log;
using Store;
using Bitub.Transfer;
using Bitub.Ifc.Transform;

namespace Task
{
    /// <summary>
    /// IFC Property set removal transformation. Will remove given property sets
    /// by their names complete from input model and returns a modifed output model.
    /// Modifications (addings and changes are tagged by editor authoring credentials). 
    /// </summary>
    [NodeName("Ifc PSetRemoval")]
    [NodeCategory("TRexIfc.Task")]
    [InPortTypes(new string[] { nameof(String), nameof(Boolean), nameof(IfcAuthorMetadata), nameof(String), nameof(IfcModel) })]
    [OutPortTypes(typeof(IfcModel))]
    [IsDesignScriptCompatible]
    public class IfcPSetRemovalTransformNodeModel : CancelableProgressingNodeModel
    {
        /// <summary>
        /// New removal node.
        /// </summary>
        public IfcPSetRemovalTransformNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("removePSetNames", "Black list of PSets about to be removed")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("caseSensitiveNames", "Enable case sensitive matching", AstFactory.BuildBooleanNode(false))));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("authorMetadata", "Credentials of authoring editor")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("canonicalName", "Fragment name of canonical full name")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("ifcModel", "IFC input model")));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("ifcModel", "IFC output model")));

            RegisterAllPorts();
            IsCancelable = true;
        }

        [JsonConstructor]
        IfcPSetRemovalTransformNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

#pragma warning disable CS1591

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            AssociativeNode[] inputs = inputAstNodes.ToArray();

            if (IsPartiallyApplied)
            {
                foreach (PortModel port in InPorts.Where(p => !p.IsConnected))
                {
                    switch (port.Index)
                    {
                        case 1:
                            inputs[port.Index] = AstFactory.BuildBooleanNode(false);
                            break;
                        default:
                            // No evalable, cancel here
                            return new[]
                            {
                                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                            };
                    }
                }
            }

            // TODO Wrap pset names into list if not already done
            if (inputs[0] is StringNode n)
            {   // Rewrite input AST 
                inputs[0] = AstFactory.BuildExprList(new List<AssociativeNode>() { inputs[0] });
            }

            // Get logger
            var callGetLogger = AstFactory.BuildFunctionCall(
                new Func<IfcModel, Logger>(IfcModel.GetLogger),
                new List<AssociativeNode>() { inputs[4] });

            // Get transform request
            var callCreateRequest = AstFactory.BuildFunctionCall(
                new Func<Logger, IfcAuthorMetadata, string[], bool, IfcTransform>(IfcTransform.RemovePropertySetsRequest),
                new List<AssociativeNode>() { callGetLogger, inputs[2], inputs[0], inputs[1] });

            // Create transformation delegate
            var callCreateIfcModelDelegate = AstFactory.BuildFunctionCall(
                new Func<IfcModel, IfcTransform, string, IfcModel>(IfcTransform.CreateIfcModelTransform),
                new List<AssociativeNode>() { inputs[4], callCreateRequest, inputs[3] });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), callCreateIfcModelDelegate)
            };

        }

#pragma warning restore CS1591
    }
}
