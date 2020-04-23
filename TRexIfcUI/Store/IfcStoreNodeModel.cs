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

namespace Store
{
    /// <summary>
    /// IFC store node as cancelable interactive node.
    /// </summary>
    [NodeName("Ifc Load")]
    [NodeCategory("TRexIfc.Store")]
    [InPortTypes(new string[] { nameof(String), nameof(Logger) })]
    [OutPortTypes(typeof(IfcStore))]
    [IsDesignScriptCompatible]
    public class IfcStoreNodeModel : CancelableCommandNodeModel
    {
        #region Internals
        // The dynamic delegate signature
        private string _ref;
        #endregion

        /// <summary>
        /// New IFC store node model.
        /// </summary>
        public IfcStoreNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("fileName", "IFC file name and path")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("logger", "Optional logger instance")));
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("store", "IFC store")));
            RegisterAllPorts();

            IsCancelable = true;
        }

        private string FunctionReference
        {
            get => null != _ref ? _ref : _ref = DynamicWrapper.Register<string, object>(CreateIfcStore);
        }

        [JsonConstructor]
        IfcStoreNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        /// <summary>
        /// A delegate for creating new Ifc stores.
        /// </summary>
        /// <param name="fileName">The IFC model file names to be loaded</param>
        /// <param name="loggerInstance">The optional logger instance</param>
        /// <returns>A new IFC producer</returns>
        public IfcStore CreateIfcStore(string fileName, object loggerInstance)
        {
            TaskName = Path.GetFileName(fileName);
            IsCanceled = false;
            return IfcStore.ByInitAndLoad(fileName, loggerInstance as Logger, Report);
        }

        /// <summary>
        /// Run the AST.
        /// </summary>
        /// <param name="inputAstNodes">Input nodes</param>
        /// <returns>Loading AST node</returns>
        [IsVisibleInDynamoLibrary(false)]
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            if (InPorts.Any(p => !p.IsConnected))
            {
                return new[]
                {
                    AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                };
            }

            var funcNode = AstFactory.BuildFunctionCall(
                new Func<string, string, object, object>(DynamicWrapper.Call),
                new List<AssociativeNode>() { AstFactory.BuildStringNode(FunctionReference), inputAstNodes[0], inputAstNodes[1] });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), funcNode)
            };
        }

    }
}
