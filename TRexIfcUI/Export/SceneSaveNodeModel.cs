using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Internal;
using TRex.Task;
using TRex.Store;
using TRex.Log;

namespace TRex.Export
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

            SelectedOption = ComponentScene.saveAsFormats.FirstOrDefault();
        }

        #region Internals

        [JsonConstructor]
        SceneSaveNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            InitOptions();
        }

        private void InitOptions()
        {
            foreach (var ext in ComponentScene.saveAsFormats)
                AvailableOptions.Add(ext);

            LogReasonMask = LogReason.Saved;
        }

        private Format SelectedFormat
        {
            get => Format.FromDynamic(SelectedOption);
        }

        #endregion

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            BeforeBuildOutputAst();

            AssociativeNode[] inputs = inputAstNodes.ToArray();

            if (null == SelectedFormat)
            {
                Warning("Format must not be null.");
                return BuildNullResult();
            }

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

                            // Not evaluable, cancel here
                            return BuildNullResult();
                    }
                }
            }

            // Create functional AST to save the given scene to file
            var astRunSaveScene = AstFactory.BuildFunctionCall(
                new Func<ComponentScene, string, string, ComponentScene>(ComponentScene.Save),
                new List<AssociativeNode>()
                {
                    inputs[0].ToDynamicTaskProgressingFunc(ProgressingTaskMethodName),
                    AstFactory.BuildStringNode(SelectedFormat?.ID),
                    inputs[1]
                });

            return BuildResult(astRunSaveScene);
        }

#pragma warning restore CS1591
    }
}
