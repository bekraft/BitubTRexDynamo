using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Task;
using TRex.Log;
using TRex.Internal;

namespace TRex.Store
{
    /// <summary>
    /// Loads an IFC model from physical file.
    /// </summary>
    [NodeName("Ifc Load")]
    [NodeCategory("TRex.Store")]
    [InPortTypes(nameof(String), nameof(Logger), nameof(IfcTessellationPrefs))]
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
            if (!IsAcceptable(inputAstNodes, 1, 2))
            {
                WarnForMissingInputs();
                ResetState();
                return BuildNullResult();
            }

            var astLoadIfcModel = AstFactory.BuildFunctionCall(
                new Func<string, Logger, IfcTessellationPrefs, IfcModel>(IfcStore.ByIfcModelFile),
                new List<AssociativeNode>()
                {
                    inputAstNodes[0],
                    inputAstNodes[1],
                    inputAstNodes[2]
                }).ToDynamicTaskProgressingFunc(ProgressingTaskMethodName);

            if (IsCanceled)
            {
                return BuildResult(AstFactory.BuildFunctionCall(
                    new Func<IfcModel, IfcModel>(IfcStore.MarkAsCancelled),
                    new List<AssociativeNode>()
                    {
                        astLoadIfcModel
                    }));
            }
            else
            {
                return BuildResult(astLoadIfcModel);
            }
        }

#pragma warning restore CS1591
    }
}
