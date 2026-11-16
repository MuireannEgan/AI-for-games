using System.Collections.Generic;
using UnityEngine;

// Holds shared configuration and runtime data for the procedural generators.
// Inspector settings remain in ForestGenerator to preserve scene references.
public sealed class ForestGenerationContext
{
    public int width, height, caveCount, minimumCaveDistance, minimumAltarCaveDistance,
        clearingRadius, intermediateWaypoints, samplesPerSection;
    public float tileSize, pathWidth, pathWidthVariation, pathCurvature,
        treeDensity, vegetationNoiseScale, clusteringStrength, minimumTreeDensity,
        maximumTreeDensity, treePositionVariation, treePathClearance,
        bushDensity, bushNoiseScale, bushClusteringStrength, bushPositionVariation,
        bushTreeClearance, bushPathClearance, bushLandmarkClearance;
    public Vector2 bushScaleRange;
    public GameObject groundTilePrefab, treePrefab, cavePrefab, altarPrefab;
    public GameObject[] bushPrefabs;
    public Material dirtPathMaterial;
    public Transform generatedParent, forestTransform;
    public List<Vector2Int> cavePositions;
    public List<GameObject> caveObjects;
    public HashSet<Vector2Int> walkablePaths;
    public List<List<Vector3>> generatedPaths;
    public GameObject AltarObject;

    public Vector3 GridToWorld(Vector2Int tile) => forestTransform.TransformPoint(
        new Vector3(tile.x * tileSize, 0f, tile.y * tileSize));
}