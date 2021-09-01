using System;
using System.Collections.Generic;
using System.Linq;

using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Task;
using TRex.Store;
using TRex.Log;

namespace TRex.Validation
{
    /// <summary>
    /// A IfcGUID validation node model.
    /// </summary>
    [NodeName("IfcGUID Validation")]
    [NodeCategory("TRex.Validation")]
    [NodeDescription("Prepares a IfcGUID validation task to check uniqueness of model GUIDs.")]
    [InPortTypes(new string[] { nameof(IfcModel), nameof(IfcGuidStore) })]
    [OutPortTypes(typeof(IfcValidationTask))]
    [IsDesignScriptCompatible]
    public class IfcGuidCheckValidationNodeModel : CancelableProgressingNodeModel
    {
        #region Internals

        [JsonConstructor]
        IfcGuidCheckValidationNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            IsCancelable = true;
            LogReasonMask = LogReason.Checked;
        }

        #endregion

        /// <summary>
        /// New GUID check validatio node model.
        /// </summary>
        public IfcGuidCheckValidationNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("ifcModel", "IFC model to be checked")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("guidStore", "GUID store instance")));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("ifcGuidCheckTask", "The validation task")));

            RegisterAllPorts();
            IsCancelable = true;
            LogReasonMask = LogReason.Checked;
        }

#pragma warning disable CS1591

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            ClearErrorsAndWarnings();
            AssociativeNode[] inputs = inputAstNodes.ToArray();
            if (IsPartiallyApplied)
            {
                WarnForMissingInputs();
                ResetState();

                // No evalable, cancel here
                return new[]
                {
                    AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                };
            }

            var callTaskCreate = AstFactory.BuildFunctionCall(
                new Func<IfcGuidStore, IfcModel, IfcValidationTask>(IfcValidationTask.NewIfcGuidCheckingTask),
                new List<AssociativeNode>() { inputs[1], inputs[0] });

            return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), callTaskCreate) };
        }

#pragma warning restore CS1591
    }
}
