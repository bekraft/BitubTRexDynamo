using System;
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
    /// IFC store producer node model.
    /// </summary>
    [NodeName("Ifc Sequential Load")]
    [NodeCategory("TRexIfc.Store")]
    [InPortTypes(new string[] { nameof(String), nameof(Logger) })]
    [OutPortTypes(typeof(IfcStoreProducer))]
    [IsDesignScriptCompatible]
    public class IfcStoreProducerNodeModel : CancelableCommandNode
    {
        #region Internals
        // The dynamic delegate signature
        private string _ref;
        private IfcStoreProducer _storeProducer;
        #endregion

        /// <summary>
        /// New IFC repository node model.
        /// </summary>
        public IfcStoreProducerNodeModel()
        {            
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("fileNames", "IFC file name and path")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("logger", "Optional logger instance")));
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("storeProducer", "IFC store producer")));
            RegisterAllPorts();

            IsCancelable = true;
        }

        private string FunctionReference
        {
            get => null != _ref ? _ref : _ref = DynamicWrapper.Register<string, object>(CreateIfcStoreProducer);
        }

        [JsonConstructor]
        IfcStoreProducerNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        /// <summary>
        /// A delegate for creating new Ifc stores.
        /// </summary>
        /// <param name="fileName">The IFC model file names to be loaded</param>
        /// <param name="loggerInstance">The logger instance</param>
        /// <returns>A new / existing IFC producer</returns>
        public IfcStoreProducer CreateIfcStoreProducer(string fileName, object loggerInstance)
        {
            if (null == _storeProducer)
            {
                _storeProducer = IfcStoreProducer.ByTaskNode(this);
                _storeProducer.FileNameCollector = new List<string>();
                _storeProducer.Logger = loggerInstance as Logger;
            }
            _storeProducer.FileNameCollector.Add(fileName);
            return _storeProducer;
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
                new Func<string, string, object>(DynamicWrapper.Call),
                new List<AssociativeNode>() { AstFactory.BuildStringNode(FunctionReference), inputAstNodes[0], inputAstNodes[1] });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), funcNode)
            };            
        }
    }
}

