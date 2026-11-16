using System;
using UnityEngine;

// Generates the ground tile grid for the forest.
public static class TerrainGenerator
{
    public static void GenerateGround(
        int width,
        int height,
        GameObject groundTilePrefab,
        Transform generatedParent,
        Func<Vector2Int, Vector3> gridToWorld)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 worldPosition = gridToWorld(new Vector2Int(x, z));

                UnityEngine.Object.Instantiate(
                    groundTilePrefab,
                    worldPosition,
                    Quaternion.identity,
                    generatedParent);
            }
        }
    }
}