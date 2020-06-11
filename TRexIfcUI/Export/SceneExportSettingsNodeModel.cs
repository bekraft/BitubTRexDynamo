using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Internal;
using Log;
using Geom;

using Bitub.Ifc.Scene;

using Dynamo.Utilities;

namespace Export
{
    /// <summary>
    /// UI Node model wrapping scene export settings.
    /// </summary>
    [NodeName("Scene Export Settings")]
    [NodeCategory("TRexIfc.Export")]
    [InPortTypes(new string[] { nameof(XYZ), nameof(Double), nameof(String), nameof(Logger) })]
    [OutPortTypes(new string[] { nameof(SceneExport) })]
    [IsDesignScriptCompatible]
    public class SceneExportSettingsNodeModel : BaseNodeModel
    {
        #region Internals

        private SceneTransformationStrategy _transformationStrategy = SceneTransformationStrategy.Quaternion;
        private ScenePositioningStrategy _positioningStrategy = ScenePositioningStrategy.NoCorrection;

        [JsonConstructor]
        SceneExportSettingsNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            Init();
        }

        private void Init()
        {
            if (null == SelectedGraphicalContext)
                SelectedGraphicalContext = new List<string>();
        }

        #endregion 

        /// <summary>
        /// New Scene Exporting Settings model.
        /// </summary>
        public SceneExportSettingsNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("offset", "Model offset as XYZ"))); // 0
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("unitsPerMeter", "Scaling units per Meter"))); // 1
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("providedContexts", "Provided representation model contexts"))); // 2
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("logger", "Logger instance"))); // 3

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("sceneExporter", "Scene exporter")));

            RegisterAllPorts();
            Init();
        }

        /// <summary>
        /// Provided grapical model contexts as selectable base set.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public ObservableCollection<string> ProvidedGraphicalContext { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Selected contexts to forward to exporter nodes.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public List<string> SelectedGraphicalContext { get; set; } = new List<string>();

        /// <summary>
        /// Transformation strategy property.
        /// </summary>
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

        /// <summary>
        /// Export position strategy property
        /// </summary>
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

        /// <summary>
        /// Add given context identifiers uniquely to already held contexts.
        /// </summary>
        /// <param name="contexts">The context identifiers to be merged</param>
        /// <returns>An array of new contexts which has been merged</returns>
        [IsVisibleInDynamoLibrary(false)]
        public string[] MergeProvidedSceneContext(params string[] contexts)
        {
            // Find new by string equality
            var newContexts = contexts
                .Where(c => !ProvidedGraphicalContext.Any(pc => pc.Equals(c, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();

            if (newContexts.Length > 0)
            {
                DispatchOnUIThread(() => ProvidedGraphicalContext.AddRange(newContexts));
                RaisePropertyChanged(nameof(ProvidedGraphicalContext));
                SynchronizeSelectedSceneContexts(SelectedGraphicalContext, true);
            }
            return newContexts;
        }

        /// <summary>
        /// Synchronizes the current selection with the previous selection using the current provided context names.
        /// </summary>
        /// <param name="contexts">Previous selection / current selection</param>
        /// <param name="forceUpdate">Whether to force a node / AST update</param>
        [IsVisibleInDynamoLibrary(false)]
        public void SynchronizeSelectedSceneContexts(IEnumerable<string> contexts, bool forceUpdate = false)
        {
            ISet<string> contextSet = new HashSet<string>(contexts ?? Enumerable.Empty<string>());
            SelectedGraphicalContext = ProvidedGraphicalContext.Where(c => contexts.Contains(c)).ToList();
            RaisePropertyChanged(nameof(SelectedGraphicalContext));
            OnNodeModified(forceUpdate);
        }

#pragma warning disable CS1591

        [IsVisibleInDynamoLibrary(false)]
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            AssociativeNode[] inputs = inputAstNodes.ToArray();

            if (IsPartiallyApplied)
            {
                foreach (PortModel port in InPorts.Where(p => !p.IsConnected))
                {
                    switch (port.Index)
                    {
                        case 1:
                            // Default units per meter = 1.0
                            inputs[1] = AstFactory.BuildDoubleNode(1.0);
                            break;
                        case 2:
                        case 3:
                            // No provided scene contexts
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

            AssociativeNode sceneContexts;
            if (SelectedGraphicalContext?.Count > 0)
                sceneContexts = AstFactory.BuildExprList(
                        SelectedGraphicalContext.Select(c => AstFactory.BuildStringNode(c) as AssociativeNode).ToList()); //new List<AssociativeNode>(){ AstFactory.BuildStringNode("Body") })
            else
                sceneContexts = AstFactory.BuildNullNode();

            var callCreateSceneExport = AstFactory.BuildFunctionCall(
                new Func<string, string, XYZ, double, string[], Logger, SceneExport>(SceneExportSettings.BySettings),                
                new List<AssociativeNode>() {
                    BuildEnumNameNode(TransformationStrategy),
                    BuildEnumNameNode(PositioningStrategy),
                    inputs[0],
                    inputs[1],
                    sceneContexts,
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
