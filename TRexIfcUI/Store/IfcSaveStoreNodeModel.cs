using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Internal;
using TRex.Task;
using TRex.Log;

namespace TRex.Store
{
    /// <summary>
    /// Saves an IFC model instance to physical file.
    /// </summary>
    [NodeName("Ifc Save")]
    [NodeCategory("TRex.Store")]
    [NodeDescription("Saves an IFC model to file by selected format extension.")]
    [InPortTypes(typeof(IfcModel))]
    [OutPortTypes(typeof(IfcModel))]
    [IsDesignScriptCompatible]
    public class IfcSaveStoreNodeModel : CancelableProgressingOptionNodeModel<string>
    {
#pragma warning disable CS1591

        public IfcSaveStoreNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("ifcModel", "Input model")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("separator", "If using canonical name, define the separator")));

            OutPorts.Add(new PortModel(PortType.Output, this, 
                new PortData("ifcModel", "Saved model")));

            RegisterAllPorts();
            IsCancelable = true;

            LogReasonMask = LogReason.Saved;
            Selected = IfcStore.Extensions[0];
        }

        [JsonConstructor]
        IfcSaveStoreNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            LogReasonMask = LogReason.Saved;
        }

        protected override IEnumerable<string> GetInitialOptions() => IfcStore.Extensions;

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
                        // 0 is mandatory
                        case 1:
                            // Default 
                            inputs[1] = AstFactory.BuildNullNode();
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

            return new[]
            {
                AstFactory.BuildAssignment(
                    GetAstIdentifierForOutputIndex(0),
                    AstFactory.BuildFunctionCall(
                        new Func<IfcModel, string, string, IfcModel>(IfcModel.SaveAs),
                        new List<AssociativeNode>() {
                            inputAstNodes[0].ToDynamicTaskProgressingFunc(ProgressingTaskMethodName),
                            AstFactory.BuildStringNode(Selected),
                            inputAstNodes[1]
                        }))
            };
        }

#pragma warning restore CS1591
    }
}
