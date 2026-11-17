using System.Collections.Generic;
using UnityEngine;

// Handles cave placement, landmark spawning and vegetation clearings.
public static class LandmarkGenerator
{
    private const float CaveRotationOffset = -90f;

    public static List<Vector2Int> GenerateCavePositions(ForestGenerationContext c,
        System.Random random,
        Vector2Int altarPosition)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        List<Vector2Int> candidates = new List<Vector2Int>();

        int margin = Mathf.Max(1, c.clearingRadius);

        for (int x = margin; x < c.width - margin; x++)
        {
            for (int z = margin; z < c.height - margin; z++)
            {
                Vector2Int candidate = new Vector2Int(x, z);
                float distanceFromAltar = Vector2Int.Distance(candidate, altarPosition);

                if (distanceFromAltar >= c.minimumAltarCaveDistance)
                    candidates.Add(candidate);
            }
        }

        // Shuffle candidates using Fisher-Yates for random cave placement.
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);

            Vector2Int temp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = temp;
        }

        foreach (Vector2Int candidate in candidates)
        {
            bool valid = true;

            foreach (Vector2Int existing in result)
            {
                if (Vector2Int.Distance(candidate, existing) < c.minimumCaveDistance)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
                continue;

            result.Add(candidate);

            if (result.Count == c.caveCount)
                break;
        }

        return result;
    }

    public static void SpawnLandmarks(ForestGenerationContext c, Vector2Int altarPosition)
    {
        for (int i = 0; i < c.cavePositions.Count; i++)
        {
            Vector3 position = c.GridToWorld(c.cavePositions[i]) + Vector3.up * 1.5f;
            Quaternion rotation = Quaternion.identity;

            if (i < c.generatedPaths.Count)
            {
                List<Vector3> path = c.generatedPaths[i];

                if (path != null && path.Count >= 2)
                {
                    Vector3 direction = path[path.Count - 2] - path[path.Count - 1];
                    direction.y = 0f;

                    if (direction.sqrMagnitude > 0.0001f)
                    {
                        rotation = Quaternion.LookRotation(direction);
                        rotation *= Quaternion.Euler(0f, CaveRotationOffset, 0f);
                    }
                }
            }

            GameObject cave = UnityEngine.Object.Instantiate(
                c.cavePrefab,
                position,
                rotation,
                c.generatedParent
            );

            cave.name = $"Cave_{i + 1}";
            c.caveObjects.Add(cave);
        }

        Vector3 altarWorldPosition = c.GridToWorld(altarPosition) + Vector3.up * 0.5f;

        c.AltarObject = UnityEngine.Object.Instantiate(
            c.altarPrefab,
            altarWorldPosition,
            Quaternion.identity,
            c.generatedParent
        );

        c.AltarObject.name = "Forest_Altar";
    }

    public static bool IsInClearing(ForestGenerationContext c, Vector2Int tile, Vector2Int altarPosition)
    {
        if (Vector2Int.Distance(tile, altarPosition) <= c.clearingRadius)
            return true;

        foreach (Vector2Int cave in c.cavePositions)
        {
            if (Vector2Int.Distance(tile, cave) <= c.clearingRadius)
                return true;
        }

        return false;
    }
}