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

#pragma warning disable CS1591
    
    #region Internals
    
    [JsonConstructor]
    MapConversionModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) 
        : base(inPorts, outPorts)
    {
    }
    
    private string? _nameOfProjectedCrs;
    private string? _descriptionOfProjectedCrs;
    private string? _geodeticDatumOfProjectedCrs;
    private string? _verticalDatumOfProjectedCrs;
    private string? _mapProjectionOfProjectedCrs;
    private string? _mapZoneOfProjectedCrs;
    private IfcSIUnitName _mapUnitNameOfProjectedCrs = IfcSIUnitName.METRE;
    private double? _scaleOfConversion;
    private bool _useLocalOffsetConversion = false;
    
    #endregion
    
    /// <summary>
    /// New map conversion model with non-valid property settings.
    /// </summary>
    public MapConversionModel() : base()
    {
        OutPorts.Add(
            new PortModel(PortType.Output, this, new PortData("data", "Map conversion data")));
        InPorts.Add(
            new PortModel(PortType.Input, this, new PortData("offset", "Offset and height on map")));
        InPorts.Add(
            new PortModel(PortType.Input, this, new PortData("xAxis", "X axis on map")));
        
        RegisterAllPorts();
    }
    
    public List<string> MapUnitNames => Enum.GetNames<IfcSIUnitName>().ToList();

    public string? NameOfProjectedCRS
    {
        get => _nameOfProjectedCrs;
        set
        {
            if (value == _nameOfProjectedCrs) return;
            _nameOfProjectedCrs = value;
            RaisePropertyChanged(nameof(NameOfProjectedCRS));
        }
    }

    public string? DescriptionOfProjectedCRS
    {
        get => _descriptionOfProjectedCrs;
        set
        {
            if (value == _descriptionOfProjectedCrs) return;
            _descriptionOfProjectedCrs = value;
            RaisePropertyChanged(nameof(DescriptionOfProjectedCRS));
        }
    }

    public string? GeodeticDatumOfProjectedCRS
    {
        get => _geodeticDatumOfProjectedCrs;
        set
        {
            if (value == _geodeticDatumOfProjectedCrs) return;
            _geodeticDatumOfProjectedCrs = value;
            RaisePropertyChanged(nameof(GeodeticDatumOfProjectedCRS));
        }
    }

    public string? VerticalDatumOfProjectedCRS
    {
        get => _verticalDatumOfProjectedCrs;
        set
        {
            if (value == _verticalDatumOfProjectedCrs) return;
            _verticalDatumOfProjectedCrs = value;
            RaisePropertyChanged(nameof(VerticalDatumOfProjectedCRS));
        }
    }

    public string? MapProjectionOfProjectedCRS
    {
        get => _mapProjectionOfProjectedCrs;
        set
        {
            if (value == _mapProjectionOfProjectedCrs) return;
            _mapProjectionOfProjectedCrs = value;
            RaisePropertyChanged(nameof(MapProjectionOfProjectedCRS));
        }
    }

    public string? MapZoneOfProjectedCRS
    {
        get => _mapZoneOfProjectedCrs;
        set
        {
            if (value == _mapZoneOfProjectedCrs) return;
            _mapZoneOfProjectedCrs = value;
            RaisePropertyChanged(nameof(MapZoneOfProjectedCRS));
        }
    }

    public IfcSIUnitName MapUnitNameOfProjectedCRS
    {
        get => _mapUnitNameOfProjectedCrs;
        set
        {
            if (value == _mapUnitNameOfProjectedCrs) return;
            _mapUnitNameOfProjectedCrs = value;
            RaisePropertyChanged(nameof(MapUnitNameOfProjectedCRS));
        }
    }

    public double? ScaleOfConversion
    {
        get => _scaleOfConversion;
        set
        {
            if (Nullable.Equals(value, _scaleOfConversion)) return;
            _scaleOfConversion = value;
            RaisePropertyChanged(nameof(ScaleOfConversion));
        }
    }

    public bool UseLocalOffsetConversion
    {
        get => _useLocalOffsetConversion;
        set
        {
            if (value == _useLocalOffsetConversion) return;
            _useLocalOffsetConversion = value;
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