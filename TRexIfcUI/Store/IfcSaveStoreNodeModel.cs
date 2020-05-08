using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Internal;
using Task;
using Log;

namespace Store
{
    /// <summary>
    /// Saves an IFC model instance to physical file.
    /// </summary>
    [NodeName("Ifc Save")]
    [NodeCategory("TRexIfc.Store")]
    [InPortTypes(typeof(IfcModel))]
    [OutPortTypes(typeof(IfcModel))]
    [IsDesignScriptCompatible]
    public class IfcSaveStoreNodeModel : CancelableProgressingOptionNodeModel
    {
        /// <summary>
        /// New save store model.
        /// </summary>
        public IfcSaveStoreNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("ifcModel", "Input model")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("separator", "If using canonical name, define the separator")));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("ifcModel", "Saved model")));

            RegisterAllPorts();
            IsCancelable = true;

            InitOptions();
            SelectedOption = IfcStore.Extensions[0];
        }

        [JsonConstructor]
        IfcSaveStoreNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            InitOptions();
        }

        private void InitOptions()
        {
            foreach (var ext in IfcStore.Extensions)
                AvailableOptions.Add(ext);
        }

#pragma warning disable CS1591

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            AssociativeNode[] inputs = inputAstNodes.ToArray();
            bool isUsingCanonicalNaming = true;

            if (IsPartiallyApplied)
            {
                foreach (PortModel port in InPorts.Where(p => !p.IsConnected))
                {
                    switch (port.Index)
                    {
                        // 0 is mandatory
                        case 1:
                            // Default 
                            isUsingCanonicalNaming = false;
                            inputs[1] = AstFactory.BuildNullNode();
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

            var callIfcModelSave = AstFactory.BuildFunctionCall(
                new Func<IfcModel, string, bool, string, IfcModel>(IfcModel.SaveAs),
                new List<AssociativeNode>() { 
                    inputAstNodes[0], 
                    AstFactory.BuildStringNode(SelectedOption as string),
                    AstFactory.BuildBooleanNode(isUsingCanonicalNaming),
                    inputAstNodes[1]
                });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), callIfcModelSave)
            };
        }

#pragma warning restore CS1591
    }
}
