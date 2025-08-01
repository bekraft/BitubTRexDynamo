using System;
using Bitub.Dto.Scene;

namespace TRex.Geom;

public sealed class UVs
{
#pragma warning disable CS1591

    #region Internals
    
    private UVs()
    { }

    #endregion

#pragma warning restore CS1591

    /// <summary>
    /// A unit vector (1,0).
    /// </summary>
    /// <returns>Unit vector.</returns>
    public static UV OneU()
    {
        return new UV() { U = 1.0f, V = 0.0f };
    }
    
    /// <summary>
    /// New UV coordinates by coordinate values.
    /// </summary>
    /// <param name="u">The U coordinate</param>
    /// <param name="v">The V coordinate</param>
    /// <returns>New UV coordinates</returns>
    public static UV ByUV(float u, float v)
    {
        return new UV() {  U = u, V = v };
    }

    /// <summary>
    /// Circular unit coordinates given an angle from right hand X-axis CCW.
    /// </summary>
    /// <param name="angle">The angle as DEG value.</param>
    /// <returns>The circular coordinates yielding a unit vector</returns>
    public static UV ByDEG(float angle)
    {
        return new UV() {  U = MathF.Cos(angle / 180), V = MathF.Sin(angle / 180) };
    }
}