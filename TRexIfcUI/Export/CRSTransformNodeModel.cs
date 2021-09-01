using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Bitub.Dto.Spatial;

using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Internal;

namespace TRex.Geom
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

            crsTransform = predefined.FirstOrDefault();
        }

        #region Internals

        internal static readonly CRSTransform[] predefined = new[]
        {
            CRSTransform.ByRighthandZUp(),
            CRSTransform.ByRighthandYUp(CRSTransform.ByRighthandZUp()),
            CRSTransform.ByLefthandYUp(CRSTransform.ByRighthandZUp()),
            CRSTransform.ByLefthandZUp(CRSTransform.ByRighthandZUp())
        };

        private CRSTransform crsTransform;

        [JsonConstructor]
        CRSTransformNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        { }

        static internal AssociativeNode BuildCRSTransformNode(CRSTransform transform, AssociativeNode sourceTransform)
        {
            if (null != transform)
            {
                return AstFactory.BuildFunctionCall(
                    new Func<string, CRSTransform, string, string, string, CRSTransform>(CRSTransform.ByData),
                    new List<AssociativeNode>()
                    {
                        AstFactory.BuildStringNode(transform.Name),
                        sourceTransform,
                        BuildEnumNameNode(transform.Right),
                        BuildEnumNameNode(transform.Up),
                        BuildEnumNameNode(transform.Forward)
                    });
            }
            else
            {
                return AstFactory.BuildNullNode();
            }
        }

        #endregion

        public CRSTransform Transform
        {
            get {
                return crsTransform;
            }
            set {
                crsTransform = value;
                RaisePropertyChanged(nameof(Transform));
                OnNodeModified(true);                
            }
        }

        [JsonIgnore]
        public ObservableCollection<CRSTransform> ItemsTransform { get; } = new ObservableCollection<CRSTransform>(predefined);

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
