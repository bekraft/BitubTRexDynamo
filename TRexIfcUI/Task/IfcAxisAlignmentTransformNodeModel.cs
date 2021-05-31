using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Log;
using TRex.Store;

using TRex.Internal;
using TRex.Geom;
using Bitub.Ifc.Transform.Requests;

namespace TRex.Task
{
    /// <summary>
    /// IFC Property set removal transformation. Will remove given property sets
    /// by their names complete from input model and returns a modifed output model.
    /// Modifications (addings and changes are tagged by editor authoring credentials). 
    /// </summary>
    [NodeName("Ifc Axis Alignment")]
    [NodeDescription("Changes the model coordinate system alignment by offset and rotation.")]
    [NodeCategory("TRex.Task")]
    [InPortTypes(new string[] { nameof(Alignment), nameof(IfcAuthorMetadata), nameof(String), nameof(LogReason), nameof(IfcModel) })]
    [OutPortTypes(typeof(IfcModel))]
    [IsDesignScriptCompatible]
    public class IfcAxisAlignmentTransformNodeModel : CancelableProgressingOptionNodeModel<string>
    {
#pragma warning disable CS1591

        public IfcAxisAlignmentTransformNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("alignment", "Axis alignment")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("authorMetadata", "Credentials of authoring editor")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("canonicalName", "Fragment name of canonical full name")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("ifcModel", "IFC input model")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("logReasonFilter", "Log reason type filtering", MapEnum(LogReason.Any))));

            OutPorts.Add(new PortModel(PortType.Output, this, 
                new PortData("ifcModel", "IFC output model")));

            RegisterAllPorts();
            
            IsCancelable = true;
            LogReasonMask = LogReason.Changed;
            Selected = PlacementOptions.Keys.First();
        }

        #region Internals

        [JsonConstructor]
        IfcAxisAlignmentTransformNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {            
            IsCancelable = true;
            LogReasonMask = LogReason.Changed;
        }

        private IDictionary<string, IfcPlacementStrategy> PlacementOptions = new Dictionary<string, IfcPlacementStrategy>()
        {
            { "Change existing placements", IfcPlacementStrategy.ChangeRootPlacements },
            { "Insert new root placement", IfcPlacementStrategy.NewRootPlacement }
        };

        protected override IEnumerable<string> GetInitialOptions() => PlacementOptions.Keys;

        #endregion

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
                        case 4:
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

            // Get logger
            var callGetLogger = AstFactory.BuildFunctionCall(
                new Func<IfcModel, Logger>(IfcModel.GetLogger),
                new List<AssociativeNode>() { inputs[3] });

            // Get current strategy selection as int index
            var strategyOption = PlacementOptions[Selected];

            // Get transform request
            var callCreateRequest = AstFactory.BuildFunctionCall(
                new Func<Logger, IfcAuthorMetadata, Alignment, object, IfcTransform>(IfcTransform.NewTransformPlacementRequest),
                new List<AssociativeNode>() { callGetLogger, inputs[1], inputs[0], BuildEnumNameNode(strategyOption) });

            // Create transformation delegate
            var callCreateIfcModelDelegate = AstFactory.BuildFunctionCall(
                new Func<IfcModel, IfcTransform, string, object, IfcModel>(IfcTransform.BySourceAndTransform),
                new List<AssociativeNode>() { inputs[3], callCreateRequest, inputs[2], inputs[4] });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), callCreateIfcModelDelegate.ToDynamicTaskProgressingFunc(ProgressingTaskMethodName))
            };
        }

#pragma warning restore CS1591

    }
}
