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
    /// Exports the scene to an 3rd party custom format indicated by extension.
    /// </summary>
    [NodeName("Save scene")]
    [NodeCategory("TRex.Export")]
    [InPortTypes(new string[] { nameof(ComponentScene), nameof(String) })]
    [OutPortTypes(typeof(ComponentScene))]
    [IsDesignScriptCompatible]
    public class SceneExportNodeModel : CancelableProgressingOptionNodeModel
    {
#pragma warning disable CS1591

        public SceneExportNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("scene", "Component scene model")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("separator", "If using canonical name, define the separator", AstFactory.BuildStringNode(""))));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("scene", "Component scene model")));

            RegisterAllPorts();
            InitOptions();

            IsCancelable = false;

            SelectedOption = ComponentScene.exportAsFormats.FirstOrDefault();
        }

        #region Internals

        [JsonConstructor]
        SceneExportNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            InitOptions();
        }

        private void InitOptions()
        {
            foreach (var ext in ComponentScene.exportAsFormats)
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

            // Create functional AST to export the given scene to file
            var astRunExportScene = AstFactory.BuildFunctionCall(
                new Func<ComponentScene, string, string, ComponentScene>(ComponentScene.Export),
                new List<AssociativeNode>()
                {
                    inputs[0].ToDynamicTaskProgressingFunc(ProgressingTaskMethodName),
                    AstFactory.BuildStringNode(SelectedOption?.ToString()),
                    inputs[1]
                });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), astRunExportScene)
            };
        }

#pragma warning restore CS1591
    }
}
