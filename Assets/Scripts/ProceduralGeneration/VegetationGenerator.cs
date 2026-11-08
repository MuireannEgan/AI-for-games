using System;
using UnityEngine;

// Handles procedural tree and bush placement using noise-based clustering.
public static class VegetationGenerator
{
    public static int GenerateTrees(
        System.Random random,
        int width,
        int height,
        GameObject treePrefab,
        Transform generatedParent,
        Transform forestTransform,
        Func<Vector2Int, Vector3> gridToWorld,
        Func<Vector2Int, bool> isReserved,
        float treeDensity,
        float vegetationNoiseScale,
        float clusteringStrength,
        float minimumTreeDensity,
        float maximumTreeDensity,
        float treePositionVariation,
        System.Collections.Generic.List<Vector3> treePositions = null)
    {
        int treeCount = 0;

        float noiseOffsetX = (float)random.NextDouble() * 10000f;
        float noiseOffsetZ = (float)random.NextDouble() * 10000f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int tile = new Vector2Int(x, z);

                if (isReserved(tile))
                    continue;

                // Perlin noise varies tree density to create natural clusters.
                float noiseValue = Mathf.PerlinNoise(
                    x * vegetationNoiseScale + noiseOffsetX,
                    z * vegetationNoiseScale + noiseOffsetZ);

                float densityVariation = (noiseValue - 0.5f) *
                    2f * clusteringStrength;

                float localDensity = Mathf.Clamp(
                    treeDensity + densityVariation,
                    minimumTreeDensity,
                    maximumTreeDensity);

                if (random.NextDouble() >= localDensity)
                    continue;

                float offsetX = ((float)random.NextDouble() * 2f - 1f) *
                    treePositionVariation;
                float offsetZ = ((float)random.NextDouble() * 2f - 1f) *
                    treePositionVariation;

                Vector3 offset = new Vector3(offsetX, 0f, offsetZ);
                Vector3 treePosition = gridToWorld(tile) +
                    forestTransform.TransformVector(offset) +
                    Vector3.up * 2.5f;

                GameObject tree = UnityEngine.Object.Instantiate(
                    treePrefab,
                    treePosition,
                    Quaternion.identity,
                    generatedParent);

                tree.name = $"Tree_{x}_{z}";

                if (treePositions != null)
                    treePositions.Add(treePosition);

                treeCount++;
            }
        }

        return treeCount;
    }

    // Uses a separate noise field and places bushes after tree generation.
    public static int GenerateBushes(
        System.Random random,
        int width,
        int height,
        GameObject[] bushPrefabs,
        Transform generatedParent,
        Transform forestTransform,
        Func<Vector2Int, Vector3> gridToWorld,
        Func<Vector2Int, bool> isReserved,
        Func<Vector3, bool> isPositionClear,
        System.Collections.Generic.IReadOnlyList<Vector3> treePositions,
        float density,
        float noiseScale,
        float clusteringStrength,
        float positionVariation,
        Vector2 scaleRange,
        float treeClearance)
    {
        if (bushPrefabs == null || bushPrefabs.Length == 0 || density <= 0f)
            return 0;

        var validPrefabs = new System.Collections.Generic.List<GameObject>();

        foreach (GameObject prefab in bushPrefabs)
        {
            if (prefab != null)
                validPrefabs.Add(prefab);
        }

        if (validPrefabs.Count == 0)
            return 0;

        int bushCount = 0;
        float noiseOffsetX = (float)random.NextDouble() * 10000f;
        float noiseOffsetZ = (float)random.NextDouble() * 10000f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int tile = new Vector2Int(x, z);

                if (isReserved(tile))
                    continue;

                float noise = Mathf.PerlinNoise(
                    x * noiseScale + noiseOffsetX,
                    z * noiseScale + noiseOffsetZ);

                float localDensity = Mathf.Clamp01(
                    density + (noise - 0.5f) * 2f * clusteringStrength);

                if (random.NextDouble() >= localDensity)
                    continue;

                Vector3 offset = new Vector3(
                    ((float)random.NextDouble() * 2f - 1f) * positionVariation,
                    0f,
                    ((float)random.NextDouble() * 2f - 1f) * positionVariation);

                Vector3 position = gridToWorld(tile) + forestTransform.TransformVector(offset);

                if (!isPositionClear(position))
                    continue;

                // Compare horizontal distances so the tree's height offset is ignored.
                bool tooCloseToTree = false;
                float minimumDistanceSq = treeClearance * treeClearance;

                if (treePositions != null)
                {
                    foreach (Vector3 treePosition in treePositions)
                    {
                        Vector2 delta = new Vector2(
                            position.x - treePosition.x,
                            position.z - treePosition.z);

                        if (delta.sqrMagnitude < minimumDistanceSq)
                        {
                            tooCloseToTree = true;
                            break;
                        }
                    }
                }

                if (tooCloseToTree)
                    continue;

                GameObject prefab = validPrefabs[random.Next(validPrefabs.Count)];
                float rotationY = (float)random.NextDouble() * 360f;
                float scaleFactor = Mathf.Lerp(
                    scaleRange.x, scaleRange.y, (float)random.NextDouble());

                GameObject bush = UnityEngine.Object.Instantiate(
                    prefab, position, Quaternion.Euler(0f, rotationY, 0f), generatedParent);

                bush.transform.localScale *= scaleFactor;
                bush.name = $"Bush_{x}_{z}";

                bushCount++;
            }
        }

        return bushCount;
    }
}