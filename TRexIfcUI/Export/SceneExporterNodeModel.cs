using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Internal;
using Task;
using Store;

namespace Export
{
    /// <summary>
    /// Scene exporter node model.
    /// </summary>
    [NodeName("Scene Export")]
    [NodeCategory("TRexIfc.Export")]
    [InPortTypes(new string[] { nameof(SceneExportSettings), nameof(IfcModel), nameof(String), nameof(String)})]
    [OutPortTypes(typeof(IfcModel))]
    [IsDesignScriptCompatible]
    public class SceneExporterNodeModel : CancelableProgressingOptionNodeModel
    {
        /// <summary>
        /// New scene exporter node model
        /// </summary>
        public SceneExporterNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("settings", "Export settings")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("ifcModel", "IFC model")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("pathName", "Export path name")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("separator", "If using canonical name, define the separator")));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("ifcModel", "model")));

            RegisterAllPorts();
            InitOptions();

            IsCancelable = true;
            SelectedOption = SceneExport.Extensions[0];
        }

        #region Internals

        [JsonConstructor]
        SceneExporterNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            InitOptions();
        }

        private void InitOptions()
        {
            foreach (var ext in SceneExport.Extensions)
                AvailableOptions.Add(ext);
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
                        case 3:
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

            var callCreateSceneExport = AstFactory.BuildFunctionCall(
                new Func<SceneExportSettings, IfcModel, SceneExport>(SceneExport.BySettingsAndModel),
                new List<AssociativeNode>() 
                { 
                    inputs[0], 
                    inputs[1]
                });

            var callSetExtension = AstFactory.BuildFunctionCall(
                new Func<SceneExport, string, SceneExport>(SceneExport.ExportAs),
                new List<AssociativeNode>()
                {
                    callCreateSceneExport,
                    AstFactory.BuildStringNode(SelectedOption.ToString())
                });

            var callRunSceneExport = AstFactory.BuildFunctionCall(
                new Func<SceneExport, string, string, IfcModel>(SceneExport.RunSceneExport),
                new List<AssociativeNode>() 
                {
                    callSetExtension.ToDynamicTaskProgressingFunc(ProgressingTaskMethodName),
                    inputs[2],
                    inputs[3]
                });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), callRunSceneExport)
            };
        }

#pragma warning restore CS1591

    }
}
