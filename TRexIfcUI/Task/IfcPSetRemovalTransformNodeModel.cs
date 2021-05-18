using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Log;
using TRex.Store;
using TRex.Internal;

namespace TRex.Task
{
    /// <summary>
    /// IFC Property set removal transformation. Will remove given property sets
    /// by their names complete from input model and returns a modifed output model.
    /// Modifications (addings and changes are tagged by editor authoring credentials). 
    /// </summary>
    [NodeName("Ifc PSet Filter")]
    [NodeDescription("Removal task which drops entire property sets by their name")]
    [NodeCategory("TRex.Task")]
    [InPortTypes(new string[] { nameof(String), nameof(String), nameof(Boolean), nameof(IfcAuthorMetadata), nameof(String), nameof(IfcModel), nameof(LogReason) })]
    [OutPortTypes(typeof(IfcModel))]
    [NodeSearchTags("ifc", "pset", "filter")]
    [IsDesignScriptCompatible]
    public class IfcPSetRemovalTransformNodeModel : CancelableProgressingNodeModel
    {
        internal enum InArguments : int
        {
            RemovePSetNames = 0, 
            KeepPSetNames = 1,
            CaseSensitiveNames = 2,
            AuthorMetadata = 3,
            CanonicalName = 4,
            IfcModel = 5,
            LogReasonFilter = 6
        }

        /// <summary>
        /// New removal node.
        /// </summary>
        public IfcPSetRemovalTransformNodeModel()
        {
            InPorts.Insert((int)InArguments.RemovePSetNames, 
                new PortModel(PortType.Input, this, new PortData("excludeNames", "List of property sets about to be removed")));
            InPorts.Insert((int)InArguments.KeepPSetNames, 
                new PortModel(PortType.Input, this, new PortData("includeNames", "List of property sets about to be kept exclusively")));
            InPorts.Insert((int)InArguments.CaseSensitiveNames,
                new PortModel(PortType.Input, this, new PortData("caseSensitiveNames", "Enable case sensitive matching", AstFactory.BuildBooleanNode(false))));
            InPorts.Insert((int)InArguments.AuthorMetadata,
                new PortModel(PortType.Input, this, new PortData("authorMetadata", "Credentials of authoring editor")));
            InPorts.Insert((int)InArguments.CanonicalName, 
                new PortModel(PortType.Input, this, new PortData("canonicalName", "Fragment name of canonical full name")));
            InPorts.Insert((int)InArguments.IfcModel, 
                new PortModel(PortType.Input, this, new PortData("ifcModel", "IFC input model")));
            InPorts.Insert((int)InArguments.LogReasonFilter, 
                new PortModel(PortType.Input, this, new PortData("logReasonFilter", "Log reason type filtering", MapEnum(LogReason.Any))));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("ifcModel", "IFC output model")));

            RegisterAllPorts();
            IsCancelable = true;
            LogReasonMask = LogReason.Changed;
        }

        [JsonConstructor]
        IfcPSetRemovalTransformNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            IsCancelable = true;
            LogReasonMask = LogReason.Changed;
        }

#pragma warning disable CS1591

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            BeforeBuildOutputAst();

            AssociativeNode[] inputs = inputAstNodes.ToArray();

            if (IsPartiallyApplied)
            {
                foreach (PortModel port in InPorts.Where(p => !p.IsConnected))
                {
                    switch (port.Index)
                    {
                        case (int)InArguments.RemovePSetNames:
                        case (int)InArguments.KeepPSetNames:
                            inputs[port.Index] = AstFactory.BuildExprList(new List<AssociativeNode>());
                            break;
                        case (int)InArguments.CaseSensitiveNames:
                            inputs[port.Index] = AstFactory.BuildBooleanNode(false);
                            break;
                        case (int)InArguments.LogReasonFilter:
                            inputs[port.Index] = MapEnum(LogReason.Any);
                            break;
                        default:
                            WarnForMissingInputs();
                            ResetState();
                            // No evalable, cancel here
                            return new[]
                            {
                                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                            };
                    }
                }
            }

            // Wrap pset names into list if not already done
            if (inputs[(int)InArguments.RemovePSetNames] is StringNode)
            {   // Rewrite input AST 
                inputs[0] = AstFactory.BuildExprList(new List<AssociativeNode>() { inputs[0] });
            }

            if (inputs[1] is StringNode)
            {   // Rewrite input AST 
                inputs[1] = AstFactory.BuildExprList(new List<AssociativeNode>() { inputs[1] });
            }

            // Get logger
            var callGetLogger = AstFactory.BuildFunctionCall(
                new Func<IfcModel, Logger>(IfcModel.GetLogger),
                new List<AssociativeNode>() { inputs[5] });

            // Get transform request
            var callCreateRequest = AstFactory.BuildFunctionCall(
                new Func<Logger, IfcAuthorMetadata, string[], string[], bool, IfcTransform>(IfcTransform.NewRemovePropertySetsRequest),
                new List<AssociativeNode>() { callGetLogger, inputs[3], inputs[0], inputs[1], inputs[2] });

            // Create transformation delegate
            var callCreateIfcModelDelegate = AstFactory.BuildFunctionCall(
                new Func<IfcModel, IfcTransform, string, object, IfcModel>(IfcTransform.BySourceAndTransform),
                new List<AssociativeNode>() { inputs[5], callCreateRequest, inputs[4], inputs[6] });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), callCreateIfcModelDelegate.ToDynamicTaskProgressingFunc(ProgressingTaskMethodName))
            };

        }

#pragma warning restore CS1591
    }
}
