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
    [NodeCategory("TRexIfc.Export")]
    [InPortTypes(new string[] { nameof(SceneExport), nameof(IfcModel), nameof(String), nameof(String)})]
    [OutPortTypes(typeof(LogMessage))]
    [IsDesignScriptCompatible]
    public class SceneExporterNodeModel : CancelableProgressingOptionNodeModel
    {
        /// <summary>
        /// New scene exporter node model
        /// </summary>
        public SceneExporterNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("sceneExporter", "Scene exporter")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("ifcModel", "IFC model")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("pathName", "Export path name")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("separator", "If using canonical name, define the separator")));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("exportSummary", "Export summaries")));

            RegisterAllPorts();
            InitOptions();

            IsCancelable = true;
            SelectedOption = SceneExport.Extensions[0];
        }

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

#pragma warning disable CS1591

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
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
                            // No evalable, cancel here
                            return new[]
                            {
                                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                            };
                    }
                }
            }

            var callRunSceneExport = AstFactory.BuildFunctionCall(
                new Func<SceneExport, IfcModel, string, string, string, LogMessage>(SceneExport.RunSceneExport),
                new List<AssociativeNode>() {
                    inputs[0],
                    inputs[1],
                    inputs[2],
                    AstFactory.BuildStringNode(SelectedOption.ToString()),
                    inputs[3] });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), callRunSceneExport)
            };
        }

#pragma warning restore CS1591

    }
}
