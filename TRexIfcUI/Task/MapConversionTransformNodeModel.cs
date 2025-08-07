using System;
using System.Collections.Generic;
using System.Linq;
using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;
using Dynamo.Graph.Nodes;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using TRex.Internal;
using TRex.Log;
using TRex.Map;
using TRex.Store;
using Double = CoreNodeModels.Input.Double;

namespace TRex.Task;

[NodeName("Ifc Map Conversion Transform")]
[NodeDescription("Inserts a IFCMAPCONVERSION entity.")]
[NodeCategory("TRex.Task")]
[InPortTypes(nameof(IfcModel), nameof(MapConversion), nameof(String), nameof(XYZ), nameof(UV), nameof(Double), nameof(String), nameof(LogReason))]
[OutPortTypes(nameof(IfcModel))]
[NodeSearchTags("ifc", "map", "conversion")]
[IsDesignScriptCompatible]
public class MapConversionTransformNodeModel : CancelableProgressingNodeModel
{
    public MapConversionTransformNodeModel()
    {
        InPorts
            .Add(new PortModel(PortType.Input, this, new PortData("ifcModel", "Ifc Model")));
        InPorts
            .Add(new PortModel(PortType.Input, this, new PortData("ifcAuthor", "Ifc Author Data")));
        InPorts
            .Add(new PortModel(PortType.Input, this, new PortData("convPrefs", "Map Conversion Preferences")));
        
        InPorts.Add(
            new PortModel(PortType.Input, this, new PortData("contexts", "Ifc Representation Contexts", AstFactory.BuildNullNode())));
        
        InPorts.Add(
            new PortModel(PortType.Input, this, new PortData("offset", "Offset and height on map", AstFactory.BuildNullNode())));
        InPorts.Add(
            new PortModel(PortType.Input, this, new PortData("xAxis", "X axis on map", AstFactory.BuildNullNode())));
        InPorts.Add(
            new PortModel(PortType.Input, this, new PortData("scale", "Scale on map", AstFactory.BuildNullNode())));
        
        InPorts.Add(
            new PortModel(PortType.Input, this, new PortData("nameAddon", "Fragment name of canonical full name", AstFactory.BuildNullNode())));
        InPorts.Add(
            new PortModel(PortType.Input, this, new PortData("logFilter", "Log reason type filtering", MapEnumIntoNode(LogReason.Any))));
        
        OutPorts
            .Add(new PortModel(PortType.Output, this, new PortData("ifcModel", "Ifc Model")));
        
        RegisterAllPorts();
        
        IsCancelable = true;
        LogReasonMask = LogReason.Changed;
    }

    [JsonConstructor]
    internal MapConversionTransformNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
    }

    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
        BeforeBuildOutputAst();
        if (!IsAcceptable(inputAstNodes, Enumerable.Range(3, 5).ToArray()))
        {
            ErrorForMissingInputs();
            OnNodeModified();
            throw new ArgumentException();
            //return BuildNullResult();
        }
        
        var n1 = AstFactory.BuildFunctionCall(
            new Func<MapConversion, XYZ?, UV?, double?, bool?, string[]?, MapConversion>(MapConversion.Append),
            new List<AssociativeNode>()
            {
                inputAstNodes[2],
                inputAstNodes[4],
                inputAstNodes[5],
                inputAstNodes[6],
                AstFactory.BuildNullNode(),
                NestingStringArray(inputAstNodes[3])
            }
        );

        var n2 = AstFactory.BuildFunctionCall(
            new Func<Logger, IfcAuthorMetadata, MapConversion, IfcTransform>(IfcTransform.NewMapConversionTransform),
            new List<AssociativeNode>()
            {
                GetLoggerFromIfcModel(inputAstNodes[0]),
                inputAstNodes[1],
                n1
            });
        
        var n3 = AstFactory.BuildFunctionCall(
            new Func<IfcModel, IfcTransform, string, object, IfcModel?>(IfcTransform.BySourceAndTransform),
            new List<AssociativeNode>() 
            { 
                inputAstNodes[0],
                n2, 
                inputAstNodes[7], 
                inputAstNodes[8] 
            }
        );       

        return BuildResult(n3.ToDynamicTaskProgressingFunc(ProgressingTaskMethodName));
    }
}