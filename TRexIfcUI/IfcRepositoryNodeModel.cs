using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Xbim.Ifc;

namespace TRexIfc
{
    /// <summary>
    /// IFC repository node model.
    /// </summary>
    [NodeName("IFC Repository")]
    [NodeCategory("TRexIfc")]
    [InPortTypes(typeof(string))]
    [OutPortTypes(typeof(IfcRepository))]
    [IsDesignScriptCompatible]
    public class IfcRepositoryNodeModel : NodeModel, IProgress<int>
    {
        #region Internals
        private int _progress = 0;
        #endregion

        /// <summary>
        /// New IFC repository node model.
        /// </summary>
        public IfcRepositoryNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("fileName", "IFC file name and path")));
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("ifcRepo", "IFC repository")));
            RegisterAllPorts();
        }

        [JsonConstructor]
        IfcRepositoryNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        /// <summary>
        /// Reports the progress.
        /// </summary>
        /// <param name="progressValue"></param>
        [IsVisibleInDynamoLibrary(false)]
        public void Report(int progressValue)
        {
            Progress = progressValue;
        }

        /// <summary>
        /// The progress value as percentage
        /// </summary>
        [JsonIgnore]
        public int Progress
        {
            get {
                return _progress;
            }
            set {
                _progress = value;
                RaisePropertyChanged(nameof(Progress));                
            }
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

            var key = DynamicWrapper.Register<string>(IfcRepository.WithProgressReporter(this).ReadFile);

            var funcNode = AstFactory.BuildFunctionCall(
                new Func<string, string, object>(DynamicWrapper.Call),
                new List<AssociativeNode>() { AstFactory.BuildStringNode(key), inputAstNodes[0] });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), funcNode)
            };            
        }
    }
}

