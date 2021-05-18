using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Task;
using Log;
using Internal;

namespace Store
{
    /// <summary>
    /// Loads an IFC model from physical file.
    /// </summary>
    [NodeName("Ifc Load")]
    [NodeCategory("TRex.Store")]
    [InPortTypes(new string[] { nameof(String), nameof(Logger), nameof(IfcTessellationPrefs) })]
    [OutPortTypes(typeof(IfcModel))]
    [IsDesignScriptCompatible]
    public class IfcLoadStoreNodeModel : CancelableProgressingNodeModel
    {
        #region Internals

        [JsonConstructor]
        IfcLoadStoreNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            IsCancelable = true;
            LogReasonMask = LogReason.Loaded;
        }

        #endregion

        /// <summary>
        /// New IFC store node model.
        /// </summary>
        public IfcLoadStoreNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("fileName", "IFC file name and path")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("logger", "Optional logger instance")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("prefs", "Tessellation preferences")));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("ifcModel", "IFC model instance")));
            
            RegisterAllPorts();

            IsCancelable = true;
            LogReasonMask = LogReason.Loaded;
        }

#pragma warning disable CS1591

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            BeforeBuildOutputAst();

            AssociativeNode[] inputs = inputAstNodes.ToArray();
            if (IsPartiallyApplied)
            {
                foreach (PortModel port in InPorts.Where(p => !p.IsConnected))
                {
                    switch (port.Index)
                    {
                        case 1:
                        case 2:
                            // Default logging off / no tessellation preferences
                            inputs[port.Index] = AstFactory.BuildNullNode();
                            break;
                        default:
                            WarnForMissingInputs();
                            ResetState();
                            // No evalable, cancel here
                            return new[]
                            {
                                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                            };
                    }
                }
            }

            var callGetOrCreateModelStore = AstFactory.BuildFunctionCall(
                new Func<string, Logger, IfcTessellationPrefs, bool, IfcModel>(IfcStore.ByIfcModelFile),
                new List<AssociativeNode>() { inputs[0], inputs[1], inputs[2], AstFactory.BuildBooleanNode(IsCanceled) });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), callGetOrCreateModelStore.ToDynamicTaskProgressingFunc(ProgressingTaskMethodName))
            };
        }

#pragma warning restore CS1591
    }
}
