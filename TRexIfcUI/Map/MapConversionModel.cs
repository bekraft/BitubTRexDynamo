using System;
using System.Collections.Generic;
using System.Linq;
using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;

using Dynamo.Graph.Nodes;

using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;

using Xbim.Ifc4.Interfaces;

using TRex.Internal;

namespace TRex.Map;

/// <summary>
/// Map conversion node model wrapping preferences for map conversion task.
/// </summary>
[NodeName("Ifc Map Conversion")]
[NodeDescription("Map conversion preferences")]
[NodeCategory("TRex.Map")]
[OutPortTypes(nameof(MapConversion))]    
[InPortTypes(nameof(XYZ), nameof(UV))]
[IsDesignScriptCompatible]
public class MapConversionModel : NodeModel
{
    private string? nameOfProjectedCrs;
    private string? descriptionOfProjectedCrs;
    private string? geodeticDatumOfProjectedCrs;
    private string? verticalDatumOfProjectedCrs;
    private string? mapProjectionOfProjectedCrs;
    private string? mapZoneOfProjectedCrs;
    private IfcSIUnitName mapUnitNameOfProjectedCrs = IfcSIUnitName.METRE;
    private double? scaleOfConversion;
    private bool useLocalOffsetConversion = false;

#pragma warning disable CS1591
    
    #region Internals
    
    [JsonConstructor]
    MapConversionModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) 
        : base(inPorts, outPorts)
    {
    }

    List<IfcSIUnitName> MapUnitNames => Enum.GetValues<IfcSIUnitName>().ToList();
    
    #endregion
    
    /// <summary>
    /// New map conversion model with non-valid property settings.
    /// </summary>
    public MapConversionModel() : base()
    {
        OutPorts.Add(
            new PortModel(PortType.Output, this, new PortData("mapConversion", "Map conversion data")));
        InPorts.Add(
            new PortModel(PortType.Input, this, new PortData("offsetOnMap", "Offset on map")));
        InPorts.Add(
            new PortModel(PortType.Input, this, new PortData("xAxisOnMap", "X axis on map")));
        
        RegisterAllPorts();
    }

    public string? NameOfProjectedCRS
    {
        get => nameOfProjectedCrs;
        set
        {
            if (value == nameOfProjectedCrs) return;
            nameOfProjectedCrs = value;
            RaisePropertyChanged(nameof(NameOfProjectedCRS));
        }
    }

    public string? DescriptionOfProjectedCRS
    {
        get => descriptionOfProjectedCrs;
        set
        {
            if (value == descriptionOfProjectedCrs) return;
            descriptionOfProjectedCrs = value;
            RaisePropertyChanged(nameof(DescriptionOfProjectedCRS));
        }
    }

    public string? GeodeticDatumOfProjectedCRS
    {
        get => geodeticDatumOfProjectedCrs;
        set
        {
            if (value == geodeticDatumOfProjectedCrs) return;
            geodeticDatumOfProjectedCrs = value;
            RaisePropertyChanged(nameof(GeodeticDatumOfProjectedCRS));
        }
    }

    public string? VerticalDatumOfProjectedCRS
    {
        get => verticalDatumOfProjectedCrs;
        set
        {
            if (value == verticalDatumOfProjectedCrs) return;
            verticalDatumOfProjectedCrs = value;
            RaisePropertyChanged(nameof(VerticalDatumOfProjectedCRS));
        }
    }

    public string? MapProjectionOfProjectedCRS
    {
        get => mapProjectionOfProjectedCrs;
        set
        {
            if (value == mapProjectionOfProjectedCrs) return;
            mapProjectionOfProjectedCrs = value;
            RaisePropertyChanged(nameof(MapProjectionOfProjectedCRS));
        }
    }

    public string? MapZoneOfProjectedCRS
    {
        get => mapZoneOfProjectedCrs;
        set
        {
            if (value == mapZoneOfProjectedCrs) return;
            mapZoneOfProjectedCrs = value;
            RaisePropertyChanged(nameof(MapZoneOfProjectedCRS));
        }
    }

    public IfcSIUnitName MapUnitNameOfProjectedCRS
    {
        get => mapUnitNameOfProjectedCrs;
        set
        {
            if (value == mapUnitNameOfProjectedCrs) return;
            mapUnitNameOfProjectedCrs = value;
            RaisePropertyChanged(nameof(MapUnitNameOfProjectedCRS));
        }
    }

    public double? ScaleOfConversion
    {
        get => scaleOfConversion;
        set
        {
            if (Nullable.Equals(value, scaleOfConversion)) return;
            scaleOfConversion = value;
            RaisePropertyChanged(nameof(ScaleOfConversion));
        }
    }

    public bool UseLocalOffsetConversion
    {
        get => useLocalOffsetConversion;
        set
        {
            if (value == useLocalOffsetConversion) return;
            useLocalOffsetConversion = value;
            RaisePropertyChanged(nameof(UseLocalOffsetConversion));
        }
    }

    /// <summary>
    /// Validate properties and display warning.
    /// </summary>
    /// <returns>True, if essential properties have been set</returns>
    private bool IsValid => !string.IsNullOrWhiteSpace(NameOfProjectedCRS)
                            && !string.IsNullOrWhiteSpace(GeodeticDatumOfProjectedCRS)
                            && !string.IsNullOrWhiteSpace(MapProjectionOfProjectedCRS);

    private IEnumerable<AssociativeNode> BuildNullAssignment()
    {
        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
    }
    
    /// <summary>
    /// <inheritdoc cref="NodeModel.BuildOutputAst"/>
    /// </summary>
    /// <param name="inputAstNodes">The input nodes</param>
    /// <returns>Assignment output</returns>
    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
        ClearErrorsAndWarnings();
        if (IsPartiallyApplied)
        {
            return BuildNullAssignment();
        }

        if (!IsValid)
        {
            Warning($"Some values are missing. At least a name, a geodetic datum and a projection have to be set.");
            return BuildNullAssignment();
        }

        var n1 = AstFactory.BuildFunctionCall(
            new Func<string, string, string, string, MapConversion>(MapConversion.NewMapConversion),
            new List<AssociativeNode>()
            {
                AstFactory.BuildStringNode(NameOfProjectedCRS),
                AstFactory.BuildStringNode(GeodeticDatumOfProjectedCRS),
                AstFactory.BuildStringNode(MapProjectionOfProjectedCRS),
                MapUnitNameOfProjectedCRS.ToEnumNameNode()
            }
        );
        
        var n2 = AstFactory.BuildFunctionCall(
            new Func<MapConversion, string, string, string, MapConversion>(MapConversion.Append),
            new List<AssociativeNode>()
            {
                n1,
                AstFactory.BuildStringNode(DescriptionOfProjectedCRS),
                AstFactory.BuildStringNode(VerticalDatumOfProjectedCRS),
                AstFactory.BuildStringNode(MapZoneOfProjectedCRS)
            }
        );
        
        var n3 = AstFactory.BuildFunctionCall(
            new Func<MapConversion, XYZ, UV, Double, bool, MapConversion>(MapConversion.Append),
            new List<AssociativeNode>()
            {
                n2,
                inputAstNodes[0],
                inputAstNodes[1],
                AstFactory.BuildDoubleNode(ScaleOfConversion ?? 1.0),
                AstFactory.BuildBooleanNode(UseLocalOffsetConversion)
            }
        );

        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), n3) };
    }
    
#pragma warning restore CS1591
}