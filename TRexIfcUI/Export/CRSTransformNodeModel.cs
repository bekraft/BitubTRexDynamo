using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Bitub.Dto.Spatial;

using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;
using TRex.Geom;
using TRex.Internal;

namespace TRex.Export
{
    /// <summary>
    /// Unit scale node.
    /// </summary>
    [NodeName("CRS transform")]
    [NodeDescription("Coordinate reference transform creation node used by build and export nodes.")]
    [NodeCategory("TRex.Geom")]
    [InPortTypes(nameof(CRSTransform), nameof(XYZ))]
    [OutPortTypes(typeof(CRSTransform))]
    [IsDesignScriptCompatible]
    public class CRSTransformNodeModel : BaseNodeModel
    {
#pragma warning disable CS1591

        public CRSTransformNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this,
                new PortData("crs", "Source CRS", AstFactory.BuildNullNode())));
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("offset", "Offset", AstFactory.BuildNullNode())));
            OutPorts.Add(new PortModel(PortType.Output, this, 
                new PortData("crs", "Target CRS")));

            RegisterAllPorts();

            crsTransform = Predefined.FirstOrDefault();
        }

        #region Internals

        private static readonly CRSTransform[] Predefined = 
        {
            CRSTransform.ByRighthandZUp(),
            CRSTransform.ByRighthandYUp(),
            CRSTransform.ByLefthandYUp(),
            CRSTransform.ByLefthandZUp(),
            CRSTransform.ByMirrorX(),
            CRSTransform.ByMirrorY(), 
            CRSTransform.ByMirrorZ() 
        };

        private CRSTransform crsTransform;

        [JsonConstructor]
        CRSTransformNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        { }

        private static AssociativeNode BuildCRSTransformNode(CRSTransform transform, AssociativeNode sourceTransform)
        {
            if (null != transform)
            {
                return AstFactory.BuildFunctionCall(
                    new Func<CRSTransform, string, string, CRSTransform>(CRSTransform.ByData),
                    new List<AssociativeNode>()
                    {
                        sourceTransform,
                        AstFactory.BuildStringNode(transform.Name),
                        AstFactory.BuildStringNode(transform.Serialized)
                    });
            }
            else
            {
                return AstFactory.BuildNullNode();
            }
        }

        #endregion

        /// <summary>
        /// The current selected transform.
        /// </summary>
        public CRSTransform Transform
        {
            get => crsTransform;
            set {
                crsTransform = Predefined.FirstOrDefault(t => t.Name == value.Name);
                RaisePropertyChanged(nameof(Transform));
                OnNodeModified(true);                
            }
        }

        [JsonIgnore]
        public ObservableCollection<CRSTransform> ItemsTransform { get; } = new ObservableCollection<CRSTransform>(Predefined);

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {            
            ClearErrorsAndWarnings();

            if (null == Transform)
            {
                Warning("Transform must be selected!");
                return BuildNullResult();
            }

            return BuildResult(AstFactory.BuildFunctionCall(
                new Func<CRSTransform, XYZ, CRSTransform>(CRSTransform.ByOffsetTo), // Use offset if given
                new List<AssociativeNode>()
                {
                    BuildCRSTransformNode(Transform, inputAstNodes[0]),
                    inputAstNodes[1]
                }
            ));
        }

#pragma warning restore CS1591
    }
}
