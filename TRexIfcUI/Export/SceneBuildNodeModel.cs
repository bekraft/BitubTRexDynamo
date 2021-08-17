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
    /// Scene exporter node model.
    /// </summary>
    [NodeName("Build scene")]
    [NodeCategory("TRex.Export")]
    [InPortTypes(new string[] { nameof(SceneBuildSettings), nameof(IfcModel)})]
    [OutPortTypes(typeof(ComponentScene))]
    [IsDesignScriptCompatible]
    public class SceneBuildNodeModel : CancelableProgressingNodeModel
    {
        /// <summary>
        /// New scene exporter node model
        /// </summary>
        public SceneBuildNodeModel()
        {
            InPorts.Add(
                new PortModel(PortType.Input, this, new PortData("settings", "Build settings")));
            InPorts.Add(
                new PortModel(PortType.Input, this, new PortData("ifcModel", "IFC model")));

            OutPorts.Add(
                new PortModel(PortType.Output, this, new PortData("scene", "Component scene")));

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

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAst)
        {
            BeforeBuildOutputAst();

            if (IsPartiallyApplied)
            {
                foreach (PortModel port in InPorts.Where(p => !p.IsConnected))
                {
                    switch (port.Index)
                    {
                        default:
                            WarnForMissingInputs();
                            ResetState();
                            return BuildNullResult();
                    }
                }
            }

            // Create functional AST to create a new scene builder
            var astCreateSceneExport = AstFactory.BuildFunctionCall(
                new Func<SceneBuildSettings, IfcModel, ComponentSceneBuild>(ComponentSceneBuild.BySettingsAndModel),
                new List<AssociativeNode>() 
                { 
                    inputAst[0], 
                    inputAst[1]
                });

            // Create a functional AST to run the final builder wrapping the progressing information
            var astRunBuildComponentScene = AstFactory.BuildFunctionCall(
                new Func<ComponentSceneBuild, TimeSpan?, ComponentScene>(ComponentSceneBuild.RunBuildComponentScene),
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
