using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

namespace TRexIfc
{
    /// <summary>
    /// IFC store node model.
    /// </summary>
    [NodeName("IFC Procedural Load")]
    [NodeCategory("TRexIfc.Load")]
    [InPortTypes(typeof(string[]))]
    [OutPortTypes(typeof(IfcStoreProducer))]
    [IsDesignScriptCompatible]
    public class IfcStoreLoadNodeModel : CancelableCommandNode
    {
        #region Internals
        // The dynamic delegate signature
        private string _ref;
        private IfcStoreProducer _storeProducer;
        #endregion

        /// <summary>
        /// New IFC repository node model.
        /// </summary>
        public IfcStoreLoadNodeModel()
        {            
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("fileNames", "IFC file name and path")));
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("storeProducer", "IFC store producer")));
            RegisterAllPorts();

            IsCancelable = true;            
        }

        internal string CreateFunctionReference
        {
            get => null != _ref ? _ref : _ref = DynamicWrapper.Register<string>(CreateIfcStore);
        }

        [JsonConstructor]
        IfcStoreLoadNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        /// <summary>
        /// A delegate for creating new Ifc stores.
        /// </summary>
        /// <param name="fileName">The IFC model file names to be loaded</param>
        /// <returns>A new IFC producer</returns>
        public IfcStoreProducer CreateIfcStore(string fileName)
        {
            if (null == _storeProducer)
            {
                _storeProducer = IfcStoreProducer.ByTaskNode(this);
                _storeProducer.FileNameCollector = new List<string>();
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
                new List<AssociativeNode>() { AstFactory.BuildStringNode(CreateFunctionReference), inputAstNodes[0] });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), funcNode)
            };            
        }
    }
}

