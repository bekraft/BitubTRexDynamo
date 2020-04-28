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
            Init();
        }

        [JsonConstructor]
        IfcStoreNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            Init();
        }

        private void Init()
        {
            
        }

        /// <summary>
        /// A delegate for creating new Ifc stores.
        /// </summary>
        /// <param name="fileName">The IFC model file names to be loaded</param>
        /// <param name="loggerInstance">The optional logger instance</param>
        /// <returns>A new IFC producer</returns>
        public IfcStore CreateIfcStore(object fileName, object loggerInstance)
        {
            TaskName = Path.GetFileName(fileName as string);
            IsCanceled = false;
            return IfcStore.ByInitAndLoad(fileName as string, loggerInstance as Logger, Report);
        }

        /// <summary>
        /// Run the AST.
        /// </summary>
        /// <param name="inputAstNodes">Input nodes</param>
        /// <returns>Loading AST node</returns>
        [IsVisibleInDynamoLibrary(false)]
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            if (IsPartiallyApplied)
            {
                return new[]
                {
                    AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                };
            }

            var delegateNode = AstFactory.BuildStringNode(GlobalDelegationService.Put<object, object>(CreateIfcStore));

            var funcNode = AstFactory.BuildFunctionCall(
                new Func<string, object, object, object>(GlobalDelegationService.Call),
                new List<AssociativeNode>() { delegateNode, inputAstNodes[0], inputAstNodes[1] });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), funcNode)
            };
        }

    }
}
