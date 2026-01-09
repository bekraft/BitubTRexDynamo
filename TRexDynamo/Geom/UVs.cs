using System;
using Bitub.Dto.Scene;

namespace TRex.Geom;

public sealed class UVs
{
    #region Internals
    
    private UVs()
    { }

    #endregion

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
    /// Circular unit coordinates given an angle as DEG from right hand X-axis CCW.
    /// </summary>
    /// <param name="angle">The angle as DEG value.</param>
    /// <returns>The circular coordinates yielding a unit vector</returns>
    public static UV ByDEG(float angle)
    {
        return new UV() {  U = MathF.Cos(angle / 180 * MathF.PI), V = MathF.Sin(angle / 180 * MathF.PI) };
    }

    /// <summary>
    /// Circular unit coordinates given an angle as radians from right hand X-axis CCW.
    /// </summary>
    /// <param name="rad">The angle as radian measure</param>
    /// <returns>The circular coordinates yielding a unit vector</returns>
    public static UV ByRAD(float rad)
    {
        return new UV() {  U = MathF.Cos(rad), V = MathF.Sin(rad) };
    }
}