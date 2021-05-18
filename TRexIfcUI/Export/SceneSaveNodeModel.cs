using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Internal;
using Task;
using Store;
using Log;

namespace Export
{
    /// <summary>
    /// Saves the component scene to the requested format extension. Currently supports JSON and binary
    /// formats of protobuf specification.
    /// </summary>
    [NodeName("Save scene")]
    [NodeCategory("TRex.Export")]
    [InPortTypes(new string[] { nameof(ComponentScene), nameof(String) })]
    [OutPortTypes(typeof(ComponentScene))]
    [IsDesignScriptCompatible]
    public class SceneSaveNodeModel : CancelableProgressingOptionNodeModel
    {
#pragma warning disable CS1591

        public SceneSaveNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("scene", "Component scene model")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("separator", "If using canonical name, define the separator")));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("scene", "Component scene model")));

            RegisterAllPorts();
            InitOptions();

            IsCancelable = false;

            SelectedOption = ComponentScene.saveAsExtensions[0];
        }

        #region Internals

        [JsonConstructor]
        SceneSaveNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            InitOptions();
        }

        private void InitOptions()
        {
            foreach (var ext in ComponentScene.saveAsExtensions)
                AvailableOptions.Add(ext);

            LogReasonMask = LogReason.Saved;
        }

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
                        case 1:
                            // Canonical separator is optional
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

            // Create functional AST to save the given scene to file
            var astRunSaveScene = AstFactory.BuildFunctionCall(
                new Func<ComponentScene, string, string, ComponentScene>(ComponentScene.Save),
                new List<AssociativeNode>()
                {
                    inputs[0].ToDynamicTaskProgressingFunc(ProgressingTaskMethodName),
                    AstFactory.BuildStringNode(SelectedOption?.ToString()),
                    inputs[1]
                });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), astRunSaveScene)
            };
        }

#pragma warning restore CS1591
    }
}
