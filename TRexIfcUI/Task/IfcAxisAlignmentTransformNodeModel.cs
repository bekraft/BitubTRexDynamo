using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Log;
using TRex.Store;

using TRex.Internal;
using TRex.Geom;

using Bitub.Ifc.Transform;

namespace TRex.Task
{
    /// <summary>
    /// IFC Property set removal transformation. Will remove given property sets
    /// by their names complete from input model and returns a modifed output model.
    /// Modifications (addings and changes are tagged by editor authoring credentials). 
    /// </summary>
    [NodeName("Ifc Axis Alignment")]
    [NodeDescription("Changes the model coordinate system alignment by offset and rotation.")]
    [NodeCategory("TRex.Task")]
    [InPortTypes(nameof(Alignment), nameof(IfcAuthorMetadata), nameof(String), nameof(LogReason), nameof(IfcModel))]
    [InPortDescriptions("Axis alignment", "Author metadata", "Canonical name extension", "Log filter flag", "Input model")]
    [OutPortTypes(nameof(IfcModel))]    
    [NodeSearchTags("ifc", "placement", "alignment")]
    [IsDesignScriptCompatible]
    public class IfcAxisAlignmentTransformNodeModel : CancelableProgressingOptionNodeModel<string>
    {
#pragma warning disable CS1591

        public IfcAxisAlignmentTransformNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("alignment", "Axis alignment")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("author", "Credentials of authoring editor")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("nameAddon", "Fragment name of canonical full name")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("ifcModel", "IFC input model")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("logFilter", "Log reason type filtering", MapEnum(LogReason.Any))));

            OutPorts.Add(new PortModel(PortType.Output, this, 
                new PortData("ifcModel", "IFC output model")));

            RegisterAllPorts();
           
            IsCancelable = true;
            LogReasonMask = LogReason.Changed;
            Selected = PlacementOptions.Keys.First();
        }

        #region Internals

        [JsonConstructor]
        IfcAxisAlignmentTransformNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {            
            IsCancelable = true;
            LogReasonMask = LogReason.Changed;
        }

        private IDictionary<string, ModelPlacementStrategy> PlacementOptions = new Dictionary<string, ModelPlacementStrategy>()
        {
            { "Change existing placements", ModelPlacementStrategy.ChangeRootPlacements },
            { "Insert new root placement", ModelPlacementStrategy.NewRootPlacement }
        };

        protected override IEnumerable<string> GetInitialOptions() => PlacementOptions.Keys;

        #endregion

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAst)
        {
            BeforeBuildOutputAst();

            if (!IsAcceptable(inputAst))
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
                    inputAst[3] 
                }
            );

            AssociativeNode astPlacementOption;
            if (null == Selected)
                astPlacementOption = AstFactory.BuildNullNode();
            else
                astPlacementOption = BuildEnumNameNode(PlacementOptions[Selected]);

            // Get transform request
            var astCreateTransform = AstFactory.BuildFunctionCall(
                new Func<Logger, IfcAuthorMetadata, Alignment, object, IfcTransform>(IfcTransform.NewTransformPlacementRequest),
                new List<AssociativeNode>() 
                { 
                    astGetLogger, 
                    inputAst[1], 
                    inputAst[0], 
                    astPlacementOption
                }
            );

            // Create transformation delegate
            var astCreateTransformDelegate = AstFactory.BuildFunctionCall(
                new Func<IfcModel, IfcTransform, string, object, IfcModel>(IfcTransform.BySourceAndTransform),
                new List<AssociativeNode>() 
                { 
                    inputAst[3], 
                    astCreateTransform, 
                    inputAst[2], 
                    inputAst[4] 
                }
            );

            return BuildResult(astCreateTransformDelegate.ToDynamicTaskProgressingFunc(ProgressingTaskMethodName));
        }

#pragma warning restore CS1591

    }
}
