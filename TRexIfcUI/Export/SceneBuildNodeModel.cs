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
    /// Scene exporter node model.
    /// </summary>
    [NodeName("Build scene")]
    [NodeCategory("TRexIfc.Export")]
    [InPortTypes(new string[] { nameof(SceneExportSettings), nameof(IfcModel)})]
    [OutPortTypes(typeof(ComponentScene))]
    [IsDesignScriptCompatible]
    public class SceneBuildNodeModel : CancelableProgressingNodeModel
    {
        /// <summary>
        /// New scene exporter node model
        /// </summary>
        public SceneBuildNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("settings", "Export settings")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("ifcModel", "IFC model")));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("scene", "Component scene")));

            RegisterAllPorts();

            IsCancelable = true;
        }

        #region Internals

        [JsonConstructor]
        SceneBuildNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        #endregion

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

            // Create functional AST to create a new scene builder
            var astCreateSceneExport = AstFactory.BuildFunctionCall(
                new Func<SceneExportSettings, IfcModel, SceneExport>(SceneExport.BySettingsAndModel),
                new List<AssociativeNode>() 
                { 
                    inputs[0], 
                    inputs[1]
                });

            // Create a functional AST to run the final builder wrapping the progressing information
            var astRunBuildComponentScene = AstFactory.BuildFunctionCall(
                new Func<SceneExport, TimeSpan?, ComponentScene>(SceneExport.RunBuildComponentScene),
                new List<AssociativeNode>() 
                {
                    astCreateSceneExport.ToDynamicTaskProgressingFunc(ProgressingTaskMethodName),
                    AstFactory.BuildNullNode()
                });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), astRunBuildComponentScene)
            };
        }

#pragma warning restore CS1591

    }
}
