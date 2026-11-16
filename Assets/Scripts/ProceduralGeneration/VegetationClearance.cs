using System.Collections.Generic;
using UnityEngine;

// Checks whether bushes can spawn without obstructing paths or landmarks.
public static class VegetationClearance
{
    public static bool IsBushPositionClear(ForestGenerationContext c, Vector3 worldPosition)
    {
        return IsPositionClear(c, worldPosition, c.bushPathClearance, c.bushLandmarkClearance);
    }

    public static bool IsGroundCoverPositionClear(
        ForestGenerationContext c,
        Vector3 worldPosition,
        float pathClearance,
        float landmarkClearance)
    {
        return IsPositionClear(c, worldPosition, pathClearance, landmarkClearance);
    }

    private static bool IsPositionClear(
        ForestGenerationContext c,
        Vector3 worldPosition,
        float pathClearance,
        float landmarkClearance)
    {
        Vector3 localPosition = c.forestTransform.InverseTransformPoint(worldPosition);

        float pathBuffer = c.pathWidth * (1f + c.pathWidthVariation) * 0.5f
            + pathClearance;

        foreach (List<Vector3> path in c.generatedPaths)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (PathGenerator.DistanceToSegment(localPosition, path[i], path[i + 1]) < pathBuffer)
                    return false;
            }
        }

        Vector2 localXZ = new Vector2(localPosition.x, localPosition.z);
        Vector3 altarLocal = new Vector3(c.width / 2 * c.tileSize, 0f, c.height / 2 * c.tileSize);

        if (Vector2.Distance(localXZ, new Vector2(altarLocal.x, altarLocal.z)) < landmarkClearance)
            return false;

        foreach (Vector2Int cave in c.cavePositions)
        {
            Vector2 caveXZ = new Vector2(cave.x * c.tileSize, cave.y * c.tileSize);

            if (Vector2.Distance(localXZ, caveXZ) < landmarkClearance)
                return false;
        }

        return true;
    }
}