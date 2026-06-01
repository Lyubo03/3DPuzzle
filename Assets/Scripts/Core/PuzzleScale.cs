using UnityEngine;

/// <summary>
/// Pure helpers for interpreting world-unit distances when the puzzle is displayed
/// under a uniformly-scaled root transform (AR tabletop scale).
/// </summary>
public static class PuzzleScale
{
    /// <summary>
    /// Convert a distance defined in original world units into the equivalent distance
    /// after the puzzle root has been uniformly scaled by <paramref name="rootScale"/>.
    /// Non-positive scales fall back to the original value (identity).
    /// </summary>
    public static float ScaledDistance(float worldDistance, float rootScale)
    {
        if (rootScale <= 0f) return worldDistance;
        return worldDistance * rootScale;
    }
}
