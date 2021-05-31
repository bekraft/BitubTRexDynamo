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
    [InPortTypes(new string[] { nameof(XYZ), nameof(UnitScale), nameof(String), nameof(CanonicalFilter) })]
    [OutPortTypes(new string[] { nameof(SceneBuildSettings) })]
    [IsDesignScriptCompatible]
    public class SceneBuildSettingsNodeModel : BaseNodeModel
    {
#pragma warning disable CS1591

        #region Internals

        private SceneTransformationStrategy _transformationStrategy = SceneTransformationStrategy.Quaternion;
        private ScenePositioningStrategy _positioningStrategy = ScenePositioningStrategy.NoCorrection;

        [JsonConstructor]
        SceneBuildSettingsNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        #endregion 

        public SceneBuildSettingsNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("offset", "Model offset as XYZ"))); // 0
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("unitScale", "Scaling units per Meter", UnitScaleNodeModel.BuildUnitScaleNode(UnitScale.ByUnitsPerMeter(1.0f))))); // 1
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("providedContexts", "Provided representation model contexts"))); // 2
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("featureClassificationFilter", "Feature-2-classification filter"))); // 2

            OutPorts.Add(new PortModel(PortType.Output, this, 
                new PortData("settings", "Scene build settings")));

            RegisterAllPorts();
        }

        [IsVisibleInDynamoLibrary(false)]
        public SceneTransformationStrategy TransformationStrategy
        {
            get {
                return _transformationStrategy;
            }
            set {
                _transformationStrategy = value;
                RaisePropertyChanged(nameof(TransformationStrategy));
                OnNodeModified(false);
            }
        }

        [IsVisibleInDynamoLibrary(false)]
        public ScenePositioningStrategy PositioningStrategy
        {
            get {
                return _positioningStrategy;
            }
            set {
                _positioningStrategy = value;
                RaisePropertyChanged(nameof(PositioningStrategy));
                OnNodeModified(false);
            }
        }

        [IsVisibleInDynamoLibrary(false)]
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            ClearErrorsAndWarnings();
            if (IsPartiallyApplied)
            {
                foreach (PortModel port in InPorts.Where(p => !p.IsConnected && !p.UsingDefaultValue))
                {
                    switch (port.Index)
                    {
                        case 0:
                            break;
                        case 3:
                            break;
                        default:
                            WarnForMissingInputs();
                            return BuildNullResult();
                    }
                }
            }

            // Wrap into list
            // Wrap pset names into list if not already done
            if (inputAstNodes[2] is StringNode)
            {   // Rewrite input AST 
                inputAstNodes[2] = AstFactory.BuildExprList(new List<AssociativeNode>() { inputAstNodes[2] });
            }

            var astBuildSettings = AstFactory.BuildFunctionCall(
                new Func<string, string, XYZ, UnitScale, string[], CanonicalFilter, SceneBuildSettings>(SceneBuildSettings.ByParameters),                
                new List<AssociativeNode>() {
                    BuildEnumNameNode(TransformationStrategy),
                    BuildEnumNameNode(PositioningStrategy),
                    inputAstNodes[0],
                    inputAstNodes[1],
                    inputAstNodes[2],
                    inputAstNodes[3]
                });

            return BuildResult(astBuildSettings);            
        }

#pragma warning restore CS1591
    }
}
