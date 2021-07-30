using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Bitub.Dto.Spatial;

using TRex.Internal;
using TRex.Geom;
using TRex.Data;

using Bitub.Ifc.Export;

namespace TRex.Export
{
    /// <summary>
    /// Scene build settings
    /// </summary>
    [NodeName("Build settings")]
    [NodeDescription("Assembles all build settings for scene generation")]
    [NodeCategory("TRex.Export")]
    [InPortTypes(new string[] { nameof(XYZ), nameof(UnitScale), nameof(String)})]
    [OutPortTypes(new string[] { nameof(SceneBuildSettings) })]
    [IsDesignScriptCompatible]
    public class SceneBuildSettingsNodeModel : BaseNodeModel
    {
#pragma warning disable CS1591

        #region Internals

        private SceneTransformationStrategy transformationStrategy = SceneTransformationStrategy.Quaternion;
        private ScenePositioningStrategy positioningStrategy = ScenePositioningStrategy.NoCorrection;
        private bool isRegeneratingGUIDs = false;

        [JsonConstructor]
        SceneBuildSettingsNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        #endregion 

        public SceneBuildSettingsNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("offset", "Model offset as XYZ")));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("unitScale", "Scaling units per Meter", UnitScaleNodeModel.BuildUnitScaleNode(UnitScale.ByUnitsPerMeter(1.0f))))); 
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("providedContexts", "Provided representation model contexts"))); 

            OutPorts.Add(new PortModel(PortType.Output, this, 
                new PortData("settings", "Scene build settings")));

            RegisterAllPorts();
        }

        public bool IsUsingEntityLabelsAsID
        {
            get
            {
                return isRegeneratingGUIDs;
            }
            set
            {
                isRegeneratingGUIDs = value;
                RaisePropertyChanged(nameof(IsUsingEntityLabelsAsID));
                OnNodeModified(true);
            }
        }

        public SceneTransformationStrategy TransformationStrategy
        {
            get 
            {
                return transformationStrategy;
            }
            set 
            {
                transformationStrategy = value;
                RaisePropertyChanged(nameof(TransformationStrategy));
                OnNodeModified(true);
            }
        }

        public ScenePositioningStrategy PositioningStrategy
        {
            get 
            {
                return positioningStrategy;
            }
            set 
            {
                positioningStrategy = value;
                RaisePropertyChanged(nameof(PositioningStrategy));
                OnNodeModified(true);
            }
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            ClearErrorsAndWarnings();
            if (IsPartiallyApplied)
            {
                foreach (PortModel port in InPorts.Where(p => !p.IsConnected && !p.UsingDefaultValue))
                {
                    switch (port.Index)
                    {
                        default:
                            WarnForMissingInputs();
                            return BuildNullResult();
                    }
                }
            }

            // Wrap single string into list
            if (inputAstNodes[2] is StringNode)
            {   // Rewrite input AST 
                inputAstNodes[2] = AstFactory.BuildExprList(new List<AssociativeNode>() { inputAstNodes[2] });
            }

            var astBuildSettings = AstFactory.BuildFunctionCall(
                new Func<string, string, XYZ, UnitScale, string[], bool, SceneBuildSettings>(SceneBuildSettings.ByParameters),                
                new List<AssociativeNode>() {
                    BuildEnumNameNode(TransformationStrategy),
                    BuildEnumNameNode(PositioningStrategy),
                    inputAstNodes[0],
                    inputAstNodes[1],
                    inputAstNodes[2],
                    AstFactory.BuildBooleanNode(IsUsingEntityLabelsAsID)
                });

            return BuildResult(astBuildSettings);            
        }

#pragma warning restore CS1591
    }
}
