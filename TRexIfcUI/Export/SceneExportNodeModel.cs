using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Internal;
using TRex.Task;
using TRex.Log;
using TRex.Geom;

namespace TRex.Export
{
    /// <summary>
    /// Exports the scene to an 3rd party custom format indicated by extension.
    /// </summary>
    [NodeName("Export scene (assimp)")]
    [NodeDescription("Export scene via Assimp package. For more details see https://github.com/assimp/assimp.")]
    [NodeCategory("TRex.Export")]
    [InPortTypes(new string[] { nameof(ComponentScene), nameof(UnitScale), nameof(CRSTransform), nameof(String) })]
    [OutPortTypes(typeof(ComponentScene))]
    [IsDesignScriptCompatible]
    public class SceneExportNodeModel : CancelableProgressingOptionNodeModel<Format>
    {
#pragma warning disable CS1591

        public SceneExportNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("scene", "Component scene model")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("unitScale", "Scaling of exported model")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("transform", "Axes transform of exported model", AstFactory.BuildNullNode())));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("separator", "If using canonical name, define the separator", AstFactory.BuildStringNode(""))));

            OutPorts.Add(new PortModel(PortType.Output, this, 
                new PortData("scene", "Component scene model")));

            RegisterAllPorts();

            IsCancelable = false;

            Selected = ComponentScene.exportAsFormats.FirstOrDefault();
            LogReasonMask = LogReason.Saved;
        }

        #region Internals

        [JsonConstructor]
        SceneExportNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            LogReasonMask = LogReason.Saved;
        }

        protected override IEnumerable<Format> GetInitialOptions() => ComponentScene.exportAsFormats;

        #endregion

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            BeforeBuildOutputAst();

            if (!IsNotNullSelected())
                return BuildNullResult();

            if (IsPartiallyApplied)
            {
                foreach (PortModel port in InPorts.Where(p => !p.IsConnected && !p.UsingDefaultValue))
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
                            return BuildNullResult();
                    }
                }
            }

            // Create functional AST to export the given scene to file
            var astRunExportScene = AstFactory.BuildFunctionCall(
                new Func<ComponentScene, UnitScale, CRSTransform, string, string, ComponentScene>(ComponentScene.Export),
                new List<AssociativeNode>()
                {
                    inputAstNodes[0].ToDynamicTaskProgressingFunc(ProgressingTaskMethodName),
                    inputAstNodes[1],
                    inputAstNodes[2],
                    AstFactory.BuildStringNode(Selected.ID),
                    inputAstNodes[3]
                });

            return BuildResult(astRunExportScene);           
        }

#pragma warning restore CS1591
    }
}
