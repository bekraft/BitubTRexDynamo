using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Dynamo.Graph.Nodes;

using Autodesk.DesignScript.Runtime;

using TRex.Log;
using TRex.Internal;
using TRex.Task;
using ProtoCore.AST.AssociativeAST;

namespace TRex.Store
{
    /// <summary>
    /// Loads an IFC model from physical file.
    /// </summary>
    [NodeName("Ifc Filter")]
    [NodeCategory("TRex.Store")]
    [InPortTypes(new string[] { nameof(IfcModel), nameof(Int32), nameof(String), nameof(Boolean) })]
    [OutPortTypes(typeof(IfcModel))]
    [IsDesignScriptCompatible]
    public class IfcCopyStoreNodeModel : CancelableProgressingNodeModel
    {
        #region Internals

        [JsonConstructor]
        IfcCopyStoreNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        #endregion

        /// <summary>
        /// New IFC store node model.
        /// </summary>
        public IfcCopyStoreNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("IfcModel", "IFC model")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("Instances", "IFC instance indexes to extract")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("Types", "IFC entity types to extract")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("WithInverses", "If true, inverse relations are copied, too")));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("IfcModel", "Filtered new IFC model")));

            RegisterAllPorts();

            IsCancelable = true;
        }

#pragma warning disable CS1591

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            return base.BuildOutputAst(inputAstNodes);
        }

#pragma warning restore CS1591

    }
}
