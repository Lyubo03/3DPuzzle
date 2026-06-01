using NUnit.Framework;
using UnityEngine;

public class PuzzleScaleTests
{
    [Test]
    public void ScaledThreshold_HalvesWhenScaleIsHalf()
    {
        // A 1.0 world-unit threshold at 0.5 root scale should be 0.5 world units
        float result = PuzzleScale.ScaledDistance(1.0f, 0.5f);
        Assert.AreEqual(0.5f, result, 1e-5f);
    }

    [Test]
    public void ScaledThreshold_IdentityAtScaleOne()
    {
        float result = PuzzleScale.ScaledDistance(1.2f, 1.0f);
        Assert.AreEqual(1.2f, result, 1e-5f);
    }

    [Test]
    public void ScaledThreshold_ClampsNonPositiveScaleToIdentity()
    {
        // Guard against divide-by-zero / nonsense scale: fall back to the world value
        Assert.AreEqual(1.2f, PuzzleScale.ScaledDistance(1.2f, 0f), 1e-5f);
        Assert.AreEqual(1.2f, PuzzleScale.ScaledDistance(1.2f, -3f), 1e-5f);
    }
}
