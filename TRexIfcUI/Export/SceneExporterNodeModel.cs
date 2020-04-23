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
    [InPortTypes(new string[] { nameof(SceneExportSettings), nameof(IIfcStoreProducer)})]
    [OutPortTypes(typeof(string))]
    [IsDesignScriptCompatible]
    public class SceneExporterNodeModel : CancelableOptionCommandNodeModel
    {
        private static string[] FileExtensions = new string[] { ".json", ".scene" };

        #region Internal
        private string _ref;
        #endregion

        /// <summary>
        /// New scene exporter node model
        /// </summary>
        public SceneExporterNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("setting", "Exporter setting")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("storeProducer", "Store producer")));
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

        private string FunctionReference
        {
            get => null != _ref ? _ref : _ref = DynamicWrapper.Register<SceneExportSettings, IIfcStoreProducer>(SaveSceneExports);
        }

        public SceneExportSummary[] SaveSceneExports(SceneExportSettings settings, IIfcStoreProducer storeProducer)
        {

        }

        /// <summary>
        /// Builds the AST
        /// </summary>
        /// <param name="inputAstNodes">Input nodes</param>
        /// <returns>Embedded AST nodes associated with this node model</returns>
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            return base.BuildOutputAst(inputAstNodes);
        }
    }
}
