using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Internal;
using TRex.Geom;
using TRex.Data;

using Bitub.Ifc.Export;

namespace TRex.Export
{
    /// <summary>
    /// UI Node model wrapping scene export settings.
    /// </summary>
    [NodeName("Build settings")]
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
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("offset", "Model offset as XYZ"))); // 0
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("unitScale", "Scaling units per Meter"))); // 1
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("providedContexts", "Provided representation model contexts"))); // 2
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("featureClassificationFilter", "Feature-2-classification filter"))); // 2

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("settings", "Scene build settings")));

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
            AssociativeNode[] inputs = inputAstNodes.ToArray();

            if (IsPartiallyApplied)
            {
                foreach (PortModel port in InPorts.Where(p => !p.IsConnected))
                {
                    switch (port.Index)
                    {
                        case 0:
                            break;
                        case 1:
                            // Default units per meter = 1.0
                            inputs[1] = AstFactory.BuildDoubleNode(1.0);
                            break;
                        case 3:
                            break;
                        default:
                            WarnForMissingInputs();

                            // No evalable, cancel here
                            return new[]
                            {
                                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                            };
                    }
                }
            }

            // Wrap into list
            // Wrap pset names into list if not already done
            if (inputs[2] is StringNode)
            {   // Rewrite input AST 
                inputs[2] = AstFactory.BuildExprList(new List<AssociativeNode>() { inputs[2] });
            }


            var callCreateSceneExport = AstFactory.BuildFunctionCall(
                new Func<string, string, XYZ, UnitScale, string[], CanonicalFilter, SceneBuildSettings>(SceneBuildSettings.ByParameters),                
                new List<AssociativeNode>() {
                    BuildEnumNameNode(TransformationStrategy),
                    BuildEnumNameNode(PositioningStrategy),
                    inputs[0],
                    inputs[1],
                    inputs[2],
                    inputs[3]
                });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), callCreateSceneExport)
            };
        }

#pragma warning restore CS1591
    }
}
