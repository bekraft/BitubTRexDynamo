using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Internal;
using Task;
using Log;
using Geom;
using Store;

namespace Export
{
    /// <summary>
    /// Scene exporter node model.
    /// </summary>
    [NodeName("Scene Export")]
    [NodeCategory("TRexIfc.Export.SceneExport")]
    [InPortTypes(new string[] { nameof(SceneExportSettings), nameof(IfcModel), nameof(String)})]
    [OutPortTypes(typeof(SceneExportSummary))]
    [IsDesignScriptCompatible]
    public class SceneExporterNodeModel : CancelableProgressingOptionNodeModel
    {
        private static string[] FileExtensions = new string[] { ".json", ".scene" };

        /// <summary>
        /// New scene exporter node model
        /// </summary>
        public SceneExporterNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("setting", "Exporter setting")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("ifcModel", "IFC model")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("pathName", "Export path name")));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("exportSummary", "Export summaries")));

            RegisterAllPorts();
            InitOptions();

            IsCancelable = true;
            SelectedOption = FileExtensions[0];
        }

        [JsonConstructor]
        SceneExporterNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            InitOptions();
        }

        private void InitOptions()
        {
            foreach (var ext in FileExtensions)
                AvailableOptions.Add(ext);
        }

        /// <summary>
        /// Builds the AST
        /// </summary>
        /// <param name="inputAstNodes">Input nodes</param>
        /// <returns>Embedded AST nodes associated with this node model</returns>
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            if (IsPartiallyApplied)
            {
                return new[]
                {
                    AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                };
            }

            var delegateNode = AstFactory.BuildFunctionCall(
                new Func<SceneExportSettings, string, IfcModel, string, ICancelableTaskNode, SceneExportSummary>(SceneExport.RunSceneExport),
                new List<AssociativeNode>() {
                    inputAstNodes[0],
                    inputAstNodes[2],
                    inputAstNodes[1],
                    AstFactory.BuildStringNode(SelectedOption.ToString()),
                    AstFactory.BuildFunctionCall(
                        new Func<string, object>(GlobalArgumentService.GetArg), 
                        new List<AssociativeNode>(){ AstFactory.BuildStringNode(GlobalArgumentService.PutArguments(this)) })
                });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), delegateNode)
            };
        }
    }
}
