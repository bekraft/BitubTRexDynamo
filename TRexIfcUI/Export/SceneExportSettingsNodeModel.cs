using System;
using System.IO;
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
    public class SceneExportSettingsNodeModel : NodeModel
    {
        private SceneTransformationStrategy _transformationStrategy = SceneTransformationStrategy.Quaternion;
        private ScenePositioningStrategy _positioningStrategy = ScenePositioningStrategy.NoCorrection;
        private double _unitsPerMeter = 1.0;

        /// <summary>
        /// New Scene Exporting Settings model.
        /// </summary>
        public SceneExportSettingsNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("offset", "Model offset as XYZ")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("unitsPerMeter", "Scaling units per Meter")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("sceneContexts", "Using representation model contexts")));

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
            ProvidedGraphicalContext.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(ProvidedGraphicalContext));
                OnNodeModified(false);
            };
            SelectedGraphicalContext.CollectionChanged += (s, e) =>
            {                
                RaisePropertyChanged(nameof(SelectedGraphicalContext));
                OnNodeModified(false);
            };
        }

        /// <summary>
        /// Provided grapical model contexts.
        /// </summary>
        public ObservableCollection<string> ProvidedGraphicalContext { get; private set; } = new ObservableCollection<string>();

        /// <summary>
        /// Selected grapical model contexts.
        /// </summary>
        public ObservableCollection<string> SelectedGraphicalContext { get; private set; } = new ObservableCollection<string>();

        /// <summary>
        /// Units per Meter.
        /// </summary>
        public double UnitsPerMeter
        {
            get {
                return _unitsPerMeter;
            }
            set {
                _unitsPerMeter = value;
                RaisePropertyChanged(nameof(UnitsPerMeter));
                OnNodeModified(false);
            }
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
        /// <param name="contexts"></param>
        internal void PopulateSceneContext(params string[] contexts)
        {
            var set = new HashSet<string>(ProvidedGraphicalContext);
            foreach (var c in contexts)
                set.Add(c);

            ProvidedGraphicalContext.Clear();
            foreach (var c in set)
                ProvidedGraphicalContext.Add(c);
        }

        /// <summary>
        /// Populate scene context identifiers by contexts of model.
        /// </summary>
        /// <param name="ifcModel">The IFC model reference</param>
        internal void PopulateSceneContext(IModel ifcModel)
        {
            PopulateSceneContext(ifcModel.Instances
                .OfType<IIfcGeometricRepresentationContext>()
                .Where(c => !c.HasSubContexts.Any())
                .Select(c => c.ContextIdentifier?.ToString())                
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray());
        }

        /// <summary>
        /// Builds the AST
        /// </summary>
        /// <param name="inputAstNodes">Input nodes</param>
        /// <returns>Embedded AST nodes associated with this node model</returns>
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            return base.BuildOutputAst(inputAstNodes);
        }

    }
}
