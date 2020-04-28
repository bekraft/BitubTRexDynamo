using System;
using System.Collections.Generic;
using System.Linq;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Internal;
using Task;
using Store;

namespace Data
{
    [NodeName("Data Collector")]
    [NodeCategory("TRexIfc.Data")]
    [IsDesignScriptCompatible]
    public class DataCollectorNodeModel : CancelableCommandNodeModel
    {
        public DataCollectorNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("stores", "Store producer or stores")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("storeConsumers", "Store consuming delegates")));
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("dataArray", "Produced data per store")));

            RegisterAllPorts();
        }

        [JsonConstructor]
        DataCollectorNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        public object[][] InvokeCollector(object storesOrProducer)
        {
            //if (storesOrProducer is IIfcStoreProducer isp)
            return null;
        }

#pragma warning disable CS1591

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
                new Func<string, object, object>(GlobalDelegationService.Call),
                new List<AssociativeNode>() { AstFactory.BuildStringNode(GlobalDelegationService.Put<object>(InvokeCollector)), inputAstNodes[0] } );

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), funcNode)
            };
        }

#pragma warning restore CS1591 

    }
}
