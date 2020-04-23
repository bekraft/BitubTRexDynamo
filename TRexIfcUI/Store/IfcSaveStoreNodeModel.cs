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
    [NodeName("Ifc Save")]
    [NodeCategory("TRexIfc.Store")]
    [InPortNames("store")]
    [InPortTypes(new string[] { nameof(IfcStore) })]
    [OutPortNames("fileName")]
    [OutPortTypes(typeof(string))]
    [IsDesignScriptCompatible]
    public class IfcSaveStoreNodeModel : CancelableOptionCommandNodeModel
    {
        #region Internal
        private string _ref;
        #endregion

        private static string[] FileExtensions = new string[] { ".ifc", ".ifcxml", ".ifczip" };

        /// <summary>
        /// New save store model.
        /// </summary>
        public IfcSaveStoreNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("store", "Incoming store")));
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("fileName", "Written file name.")));

            RegisterAllPorts();
            IsCancelable = true;

            InitOptions();
            SelectedOption = FileExtensions[0];
        }

        [JsonConstructor]
        IfcSaveStoreNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
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
            get => null != _ref ? _ref : _ref = DynamicWrapper.Register<object>(SaveIfcStore);
        }

        /// <summary>
        /// Saving store callback
        /// </summary>
        /// <param name="store">The store</param>
        /// <returns>The full path name</returns>
        [IsVisibleInDynamoLibrary(false)]
        public string SaveIfcStore(object store)
        {            
            var ifcStore = store as IfcStore;
            if (null != ifcStore)
            {
                TaskName = ifcStore.ChangeExtension(SelectedOption as string).FileName;
                return IfcStore.Save(ifcStore, null, this.Report);
            }

            return null;
        }

        /// <summary>
        /// Builds the AST
        /// </summary>
        /// <param name="inputAstNodes">Input nodes</param>
        /// <returns>Embedded AST nodes associated with this node model</returns>
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
                new Func<string, object, object>(DynamicWrapper.Call),
                new List<AssociativeNode>() { AstFactory.BuildStringNode(FunctionReference), inputAstNodes[0] });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), funcNode)
            };
        }
    }
}
