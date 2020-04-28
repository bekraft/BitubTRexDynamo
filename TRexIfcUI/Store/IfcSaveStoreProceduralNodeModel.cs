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
    /// Sequential model save model.
    /// </summary>
    [NodeName("Ifc Sequential Save")]
    [NodeCategory("TRexIfc.Store")]
    [InPortNames("storeProducer", "suffix", "replacePattern", "replaceWith")]
    [InPortTypes(new string[] { nameof(IfcStoreProducer), nameof(String), nameof(String), nameof(String) })]
    [OutPortNames("fileNames")]
    [OutPortTypes(typeof(string[]))]
    [IsDesignScriptCompatible]
    public class IfcSaveStoreProceduralNodeModel : CancelableCommandNodeModel
    {
        /// <summary>
        /// New sequential model save.
        /// </summary>
        public IfcSaveStoreProceduralNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("storeProducer", "Incoming sequential produced models")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("replaceWith", "Replacement")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("replacePattern", "Regular expression for replacing partial file names")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("suffix", "Regular expression for replacing partial file names")));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("fileNames", "Written file names.")));

            RegisterAllPorts();
            IsCancelable = true;
        }

        [JsonConstructor]
        IfcSaveStoreProceduralNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        /// <summary>
        /// Saving store producer callback
        /// </summary>
        /// <param name="storeProducer">The store producer</param>
        /// <returns>The full path name</returns>
        [IsVisibleInDynamoLibrary(false)]
        public object SaveIfcStoreProducer(object storeProducer, object suffix, object replacePattern, object replaceWith)
        {
            List<string> fileNames = new List<string>();
            var ifcStoreProducer = storeProducer as IIfcStoreProducer;
            if (null != ifcStoreProducer)
            {
                // TODO
            }

            return fileNames.ToArray();
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

            var delegateNode = AstFactory.BuildStringNode(GlobalDelegationService.Put<object, object, object, object>(SaveIfcStoreProducer));

            var funcNode = AstFactory.BuildFunctionCall(
                new Func<string, object, object, object, object, object>(GlobalDelegationService.Call),
                new List<AssociativeNode>() { delegateNode, inputAstNodes[0], inputAstNodes[1], inputAstNodes[2], inputAstNodes[3] });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), funcNode)
            };
        }

    }
}
