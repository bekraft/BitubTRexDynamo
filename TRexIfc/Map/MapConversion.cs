using System;
using Autodesk.DesignScript.Runtime;

using Bitub.Dto;
using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;
using Bitub.Xbim.Ifc.Transform;

using Xbim.Ifc4.Interfaces;
using Enum = System.Enum;

namespace TRex.Map;

/// <summary>
/// Map Conversion Preferences.
/// </summary>
public sealed class MapConversion
{
    #region Internals
    
    internal MapConversionCrsPrefs?  CrsPrefs { get; init; }
    internal MapConversionPrefs?  Prefs  { get; init; }
    
    private MapConversion()
    {}
    
    #endregion

    #region Public methods

    /// <summary>
    /// Create a new map conversion preference.
    /// </summary>
    /// <param name="crsName">A name of map conversion</param>
    /// <param name="crsDatum">A geodetic datum</param>
    /// <param name="crsMapProjection">Map projection identifier (i.e. UTM)</param>
    /// <param name="ifcSIPrefixLabel">An Ifc SI prefix name (default null).</param>
    /// <returns></returns>
    [IsVisibleInDynamoLibrary(false)]
    public static MapConversion NewMapConversion(string crsName, 
        string crsDatum, 
        string crsMapProjection,
        string? ifcSIPrefixLabel = null)
    {
        IfcSIPrefix? ifcSIPrefix = null;
        if (!Enum.TryParse<IfcSIPrefix>(ifcSIPrefixLabel, out var ifcSIPrefixParsed))
        {
            ifcSIPrefix = null;
        }
        else
        {
            ifcSIPrefix = ifcSIPrefixParsed;
        }
        
        return new MapConversion()
        {
            CrsPrefs = new MapConversionCrsPrefs(
                crsName, 
                null, 
                crsDatum, 
                null, 
                crsMapProjection,
                null, 
                XYZ.Zero, 
                new UV(), 
                1.0, 
                ifcSIPrefix), 
            Prefs = new MapConversionPrefs(
                false, 
                new Qualifier[]{})
        };
    }
    
    /// <summary>
    /// Append & sets changed properties only.
    /// </summary>
    /// <param name="mapConversion">The map conversion instance</param>
    /// <param name="crsDescription">The CRS description</param>
    /// <param name="crsVerticalDatum">The projected CRS vertical datum</param>
    /// <param name="crsMapZone">The projected CRS map zone</param>
    /// <returns>A new map conversion preference</returns>
    [IsVisibleInDynamoLibrary(false)]
    public static MapConversion Append(MapConversion mapConversion, 
        string crsDescription, 
        string crsVerticalDatum,
        string crsMapZone)
    {
        return new MapConversion()
        {
            CrsPrefs = mapConversion.CrsPrefs?.MergeNonNullTo(
                null, 
                crsDescription, 
                null, 
                crsVerticalDatum, 
                null,
                crsMapZone, 
                null, 
                null, 
                null, 
                null), 
            Prefs = mapConversion.Prefs
        };
    }

    /// <summary>
    /// Append & sets changed properties only.
    /// </summary>
    /// <param name="mapConversion">The map conversion instance</param>
    /// <param name="offsetAndHeight">Offset & height</param>
    /// <param name="mapXAxis">Projected map X-axis as 2d vector</param>
    /// <param name="scale">A scale (default 1 if not set)</param>
    /// <param name="useLocalOffset">Whether to use local root offset as projected CRS embedding offset</param>
    /// <returns>A new map conversion preference</returns>
    [IsVisibleInDynamoLibrary(false)]
    public static MapConversion Append(MapConversion mapConversion, 
        XYZ? offsetAndHeight, UV? mapXAxis, Double? scale, bool useLocalOffset)
    {
        return new MapConversion()
        {
            CrsPrefs = mapConversion.CrsPrefs?.MergeNonNullTo(
                null, 
                null, 
                null, 
                null, 
                null,
                null, 
                offsetAndHeight, 
                mapXAxis, 
                scale, 
                null),
            Prefs = mapConversion.Prefs?.MergeNonNullTo(
                useLocalOffset,
                null) ?? new MapConversionPrefs(useLocalOffset, new Qualifier[]{})
        };
    }

    #endregion
}