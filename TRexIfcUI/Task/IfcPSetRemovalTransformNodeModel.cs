﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Log;
using TRex.Store;
using TRex.Internal;

namespace TRex.Task
{
    /// <summary>
    /// IFC Property set removal transformation. Will remove given property sets
    /// by their names complete from input model and returns a modifed output model.
    /// Modifications (addings and changes are tagged by editor authoring credentials). 
    /// </summary>
    [NodeName("Ifc PropertySet Filter")]
    [NodeDescription("PropertySetRemovalNodeDescription", typeof(Properties.Resources))]
    [NodeCategory("TRex.Task")]
    [InPortTypes(nameof(String), nameof(String), nameof(Boolean), nameof(IfcAuthorMetadata), nameof(String), nameof(LogReason), nameof(IfcModel))]
    [InPortDescriptions("Set names to exclude", "Set names to exclusively include", "Case sensitive matching", "Author metadata", "Canonical name extension", "Log filter flag", "Input model")]
    [OutPortTypes(typeof(IfcModel))]
    [NodeSearchTags("ifc", "pset", "filter")]
    [IsDesignScriptCompatible]
    public class IfcPSetRemovalTransformNodeModel : CancelableProgressingNodeModel
    {
#pragma warning disable CS1591

        public IfcPSetRemovalTransformNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("excludeSets", "List of property sets about to be removed")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("includeSets", "List of property sets about to be kept exclusively")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("caseSensitive", "Enable case sensitive matching")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("authorMetadata", "Credentials of authoring editor")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("nameAddon", "Fragment name of canonical full name")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("logFilter", "Log reason type filtering", MapEnum(LogReason.Any))));
            InPorts.Add(new PortModel(PortType.Input, this,
                new PortData("ifcModel", "IFC input model")));

            OutPorts.Add(new PortModel(PortType.Output, this, 
                new PortData("ifcModel", "IFC output model")));

            RegisterAllPorts();
            IsCancelable = true;
            LogReasonMask = LogReason.Changed;
        }

        #region Internals

        [JsonConstructor]
        IfcPSetRemovalTransformNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            IsCancelable = true;
            LogReasonMask = LogReason.Changed;
        }

        #endregion

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAst)
        {
            BeforeBuildOutputAst();

            if (!IsAcceptable(inputAst, 0, 1))
            {
                WarnForMissingInputs();
                ResetState();
                // No evalable, cancel here
                return BuildNullResult();
            }

            // Get logger
            var astGetLogger = AstFactory.BuildFunctionCall(
                new Func<IfcModel, Logger>(IfcModel.GetLogger),
                new List<AssociativeNode>() 
                { 
                    inputAst[6] 
                }
            );

            // Get transform request
            var astCreateTransform = AstFactory.BuildFunctionCall(
                new Func<Logger, IfcAuthorMetadata, string[], string[], bool?, IfcTransform>(IfcTransform.NewRemovePropertySetsRequest),
                new List<AssociativeNode>() 
                { 
                    astGetLogger, 
                    inputAst[3], 
                    TryNestStringNodeIntoList(inputAst[0]), 
                    TryNestStringNodeIntoList(inputAst[1]), 
                    inputAst[2] 
                }
            );

            // Create transformation delegate
            var astCreateTransformDelegate = AstFactory.BuildFunctionCall(
                new Func<IfcModel, IfcTransform, string, object, IfcModel>(IfcTransform.BySourceAndTransform),
                new List<AssociativeNode>() 
                { 
                    inputAst[6], 
                    astCreateTransform, 
                    inputAst[4], 
                    inputAst[5] 
                }
            );

            return BuildResult(astCreateTransformDelegate.ToDynamicTaskProgressingFunc(ProgressingTaskMethodName));
        }

#pragma warning restore CS1591
    }
}
