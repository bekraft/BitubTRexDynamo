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
using Task;
using Log;
using Geom;

using Bitub.Ifc.Scene;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Dynamo.Engine;
using System.Security.Policy;
using System.Collections;
using Dynamo.Utilities;

namespace Export
{
    /// <summary>
    /// UI Node model wrapping scene export settings.
    /// </summary>
    [NodeName("Scene Export Settings")]
    [NodeCategory("TRexIfc.Export.SceneExportSettings")]
    [InPortTypes(new string[] { nameof(XYZ), nameof(Double), nameof(String) })]
    [OutPortTypes(typeof(SceneExportSettings))]
    [IsDesignScriptCompatible]
    public class SceneExportSettingsNodeModel : BaseNodeModel
    {
        private SceneTransformationStrategy _transformationStrategy = SceneTransformationStrategy.Quaternion;
        private ScenePositioningStrategy _positioningStrategy = ScenePositioningStrategy.NoCorrection;

        /// <summary>
        /// New Scene Exporting Settings model.
        /// </summary>
        public SceneExportSettingsNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("offset", "Model offset as XYZ"))); // 0
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("unitsPerMeter", "Scaling units per Meter"))); // 1
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("providedContexts", "Provided representation model contexts"))); // 2

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("settings", "Scene export settings")));

            RegisterAllPorts();
            Init();
        }

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

        /// <summary>
        /// Provided grapical model contexts as selectable base set.
        /// </summary>
        public ObservableCollection<string> ProvidedGraphicalContext { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Selected contexts to forward to exporter nodes.
        /// </summary>
        public List<string> SelectedGraphicalContext { get; set; } = new List<string>();

        /// <summary>
        /// Gets the provided scene contexts from input node(s).
        /// </summary>
        /// <param name="engineController">Dynamo engine controller</param>
        /// <returns></returns>
        public string[] GetProvidedContextInput(EngineController engineController)
        {
            var nodes = InPorts[2].Connectors.Select(c => (c.Start.Index, c.Start.Owner));
            var ids = nodes.Select(n => n.Owner.GetAstIdentifierForOutputIndex(n.Index).Name);
            
            var data = ids.Select(id => engineController.GetMirror(id).GetData());
            return data.SelectMany(d =>
            {
                if (d.IsCollection)
                    return d.GetElements().Select(e => e.Data?.ToString());
                else
                    return new string[] { d.Data?.ToString() };
            }).ToArray();
        }

        /// <summary>
        /// Transformation strategy property.
        /// </summary>
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
        public void SynchronizeSelectedSceneContexts(IEnumerable<string> contexts, bool forceUpdate = false)
        {
            ISet<string> contextSet = new HashSet<string>(contexts ?? Enumerable.Empty<string>());
            SelectedGraphicalContext = ProvidedGraphicalContext.Where(c => contexts.Contains(c)).ToList();
            RaisePropertyChanged(nameof(SelectedGraphicalContext));
            OnNodeModified(forceUpdate);
        }

        /// <summary>
        /// Populate scene context identifiers by contexts of model.
        /// </summary>
        /// <param name="ifcModel">The IFC model reference</param>
        /// <returns>An array of new contexts which has been merged</returns>
        public string[] MergeProvidedSceneContext(IModel ifcModel)
        {
            return MergeProvidedSceneContext(ifcModel.Instances
                .OfType<IIfcGeometricRepresentationContext>()
                .Where(c => !c.HasSubContexts.Any())
                .Select(c => c.ContextIdentifier?.ToString())                
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray());
        }

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
                            // No provided scene contexts
                            break;
                        default:
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

            var delegateNode = AstFactory.BuildFunctionCall(
                new Func<string, string, XYZ, double, string[], SceneExportSettings>(SceneExportSettings.BySettings),                
                new List<AssociativeNode>() {
                    BuildEnumNameNode(TransformationStrategy),
                    BuildEnumNameNode(PositioningStrategy),
                    inputs[0],
                    inputs[1],
                    sceneContexts
                });

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), delegateNode)
            };
        }

    }
}
