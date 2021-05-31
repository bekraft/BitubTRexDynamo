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
    [InPortTypes(nameof(XYZ))]
    [OutPortTypes(typeof(CRSTransform))]
    [IsDesignScriptCompatible]
    public class CRSTransformNodeModel : BaseNodeModel
    {
#pragma warning disable CS1591

        public CRSTransformNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, 
                new PortData("offset", "Offset", AstFactory.BuildNullNode())));
            OutPorts.Add(new PortModel(PortType.Output, this, 
                new PortData("transform", "CRS transform")));

            RegisterAllPorts();

            crsTransform = predefined.FirstOrDefault();
        }

        #region Internals

        internal static readonly CRSTransform[] predefined = new[]
        {
            CRSTransform.ByRighthandZUp("Default"),
            CRSTransform.ByRighthandYUp("Righthand Y-Up"),
            CRSTransform.ByLefthandYUp("Lefthand Y-Up"),
            CRSTransform.ByLefthandZUp("Lefthand Z-Up")            
        };

        private CRSTransform crsTransform;

        [JsonConstructor]
        CRSTransformNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        { }

        static internal AssociativeNode BuildCRSTransformNode(CRSTransform transform)
        {
            if (null != transform)
            {
                return AstFactory.BuildFunctionCall(
                    new Func<string, string, string, string, CRSTransform>(CRSTransform.ByData),
                    new List<AssociativeNode>()
                    {
                        AstFactory.BuildStringNode(transform.Name),
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
                new Func<CRSTransform, XYZ, CRSTransform>(CRSTransform.ByOffsetTo),
                new List<AssociativeNode>()
                {
                    BuildCRSTransformNode(Transform),
                    inputAstNodes[0]
                }
            ));
        }

#pragma warning restore CS1591
    }
}
