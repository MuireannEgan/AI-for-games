using System;
using System.Collections.Generic;
using UnityEngine;

public class ForestGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField, Min(5)] private int width = 30;
    [SerializeField, Min(5)] private int height = 30;
    [SerializeField, Min(0.1f)] private float tileSize = 5f;

    [Header("Prefabs")]
    [SerializeField] private GameObject groundTilePrefab;
    [SerializeField] private GameObject treePrefab;
    [SerializeField] private GameObject cavePrefab;
    [SerializeField] private GameObject altarPrefab;

    [Header("Path Appearance")]
    [SerializeField] private Material dirtPathMaterial;
    [SerializeField, Min(0.1f)] private float pathWidth = 3f;
    [SerializeField, Range(0f, 0.3f)] private float pathWidthVariation = 0.12f;

    [Header("Path Generation")]
    [SerializeField, Range(0f, 0.5f)] private float pathCurvature = 0.14f;
    [SerializeField, Range(1, 8)] private int intermediateWaypoints = 3;
    [SerializeField, Range(4, 30)] private int samplesPerSection = 12;

    [Header("Forest Settings")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 67890;
    [SerializeField] private int currentSeed;
    [SerializeField, Range(0f, 1f)] private float treeDensity = 0.45f;

    [Header("Vegetation Generation")]
    [SerializeField, Range(0.02f, 0.5f)] private float vegetationNoiseScale = 0.12f;
    [SerializeField, Range(0f, 1f)] private float clusteringStrength = 0.65f;
    [SerializeField, Range(0f, 1f)] private float minimumTreeDensity = 0.20f;
    [SerializeField, Range(0f, 1f)] private float maximumTreeDensity = 0.80f;
    [SerializeField, Range(0f, 1f)] private float treePositionVariation = 0.70f;
    [SerializeField, Min(0f)] private float treePathClearance = 1.25f;

    [Header("Bush Generation")]
    [Tooltip("Assign one or more prefabs. Leave empty to disable bush generation.")]
    [SerializeField] private GameObject[] bushPrefabs;
    [SerializeField, Range(0f, 1f)] private float bushDensity = 0.35f;
    [SerializeField, Range(0.02f, 0.5f)] private float bushNoiseScale = 0.16f;
    [SerializeField, Range(0f, 1f)] private float bushClusteringStrength = 0.65f;
    [SerializeField, Min(0f)] private float bushPositionVariation = 1.0f;
    [SerializeField] private Vector2 bushScaleRange = new Vector2(0.8f, 1.25f);
    [SerializeField, Min(0f)] private float bushTreeClearance = 2.0f;
    [SerializeField, Min(0f)] private float bushPathClearance = 1.5f;
    [SerializeField, Min(0f)] private float bushLandmarkClearance = 7.5f;

    [Header("Cave Settings")]
    [SerializeField, Min(1)] private int caveCount = 3;
    [SerializeField, Min(0)] private int minimumCaveDistance = 7;
    [SerializeField, Min(0)] private int minimumAltarCaveDistance = 9;
    [SerializeField, Min(0)] private int clearingRadius = 1;

    [Header("Debug")]
    [SerializeField] private bool showDebugPaths = true;

    private readonly List<Vector2Int> cavePositions = new List<Vector2Int>();
    private readonly List<GameObject> caveObjects = new List<GameObject>();
    private readonly HashSet<Vector2Int> walkablePaths = new HashSet<Vector2Int>();
    private readonly List<List<Vector3>> generatedPaths = new List<List<Vector3>>();

    private Transform generatedParent;

    // Generated locations and objects are available after IsGenerated becomes true.
    // Subscribe to ForestGenerated to update references after regeneration.
    public IReadOnlyList<Vector2Int> CavePositions => cavePositions;
    public IReadOnlyList<GameObject> CaveObjects => caveObjects;
    public IReadOnlyCollection<Vector2Int> WalkablePaths => walkablePaths;

    public GameObject AltarObject { get; private set; }
    public bool IsGenerated { get; private set; }
    public int CurrentSeed => currentSeed;

    public event Action<ForestGenerator> ForestGenerated;

    private void Start()
    {
        GenerateForest();
    }

    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return transform.TransformPoint(
            new Vector3(gridPosition.x * tileSize, 0f, gridPosition.y * tileSize)
        );
    }

    // Returns the cave object's world position, not an interior spawn point.
    public Vector3 GetCaveWorldPosition(int index)
    {
        if (index < 0 || index >= caveObjects.Count)
        {
            Debug.LogError($"Invalid cave index: {index}", this);
            return Vector3.zero;
        }

        return caveObjects[index].transform.position;
    }

    // Regeneration replaces previously spawned objects and their references.
    public void GenerateForest()
    {
        if (!ValidateSettings())
            return;

        int generationSeed = useRandomSeed
            ? Guid.NewGuid().GetHashCode()
            : seed;

        System.Random random = new System.Random(generationSeed);
        Vector2Int altarPosition = new Vector2Int(width / 2, height / 2);

        // Validate cave placement before replacing the existing forest.
        ForestGenerationContext context = BuildContext();
        List<Vector2Int> newCaves = LandmarkGenerator.GenerateCavePositions(context, random, altarPosition);

        if (newCaves.Count != caveCount)
        {
            Debug.LogError(
                "Forest generation failed: unable to place all caves. Check distance constraints.",
                this
            );
            return;
        }

        IsGenerated = false;
        ClearGeneratedObjects();
        context.generatedParent = generatedParent;

        currentSeed = generationSeed;

        cavePositions.Clear();
        cavePositions.AddRange(newCaves);

        caveObjects.Clear();
        walkablePaths.Clear();
        generatedPaths.Clear();
        AltarObject = null;

        PathGenerator.GeneratePaths(context, random, altarPosition);
        int treeCount = GenerateEnvironment(context, random, altarPosition, out int bushCount);
        LandmarkGenerator.SpawnLandmarks(context, altarPosition);
        AltarObject = context.AltarObject;

        IsGenerated = true;

        Debug.Log(
            $"Forest generated. Seed: {CurrentSeed}, Grid: {width}x{height}, " +
            $"Trees: {treeCount}, Bushes: {bushCount}, Caves: {caveObjects.Count}, Paths: {generatedPaths.Count}",
            this
        );

        for (int i = 0; i < caveObjects.Count; i++)
        {
            Debug.Log(
                $"{caveObjects[i].name}: {caveObjects[i].transform.position}",
                caveObjects[i]
            );
        }

        ForestGenerated?.Invoke(this);
    }

    private ForestGenerationContext BuildContext()
    {
        return new ForestGenerationContext
        {
            width = width,
            height = height,
            tileSize = tileSize,
            groundTilePrefab = groundTilePrefab,
            treePrefab = treePrefab,
            cavePrefab = cavePrefab,
            altarPrefab = altarPrefab,
            dirtPathMaterial = dirtPathMaterial,
            pathWidth = pathWidth,
            pathWidthVariation = pathWidthVariation,
            pathCurvature = pathCurvature,
            intermediateWaypoints = intermediateWaypoints,
            samplesPerSection = samplesPerSection,
            treeDensity = treeDensity,
            vegetationNoiseScale = vegetationNoiseScale,
            clusteringStrength = clusteringStrength,
            minimumTreeDensity = minimumTreeDensity,
            maximumTreeDensity = maximumTreeDensity,
            treePositionVariation = treePositionVariation,
            treePathClearance = treePathClearance,
            bushPrefabs = bushPrefabs,
            bushDensity = bushDensity,
            bushNoiseScale = bushNoiseScale,
            bushClusteringStrength = bushClusteringStrength,
            bushPositionVariation = bushPositionVariation,
            bushScaleRange = bushScaleRange,
            bushTreeClearance = bushTreeClearance,
            bushPathClearance = bushPathClearance,
            bushLandmarkClearance = bushLandmarkClearance,
            caveCount = caveCount,
            minimumCaveDistance = minimumCaveDistance,
            minimumAltarCaveDistance = minimumAltarCaveDistance,
            clearingRadius = clearingRadius,
            generatedParent = generatedParent,
            forestTransform = transform,
            cavePositions = cavePositions,
            caveObjects = caveObjects,
            walkablePaths = walkablePaths,
            generatedPaths = generatedPaths
        };
    }

    // Use the same random sequence for paths, trees and bushes to keep seeded generation reproducible.
    private int GenerateEnvironment(ForestGenerationContext context, System.Random random,
        Vector2Int altarPosition, out int bushCount)
    {
        TerrainGenerator.GenerateGround(width, height, groundTilePrefab,
            generatedParent, GridToWorld);

        List<Vector3> treePositions = new List<Vector3>();
        int treeCount = VegetationGenerator.GenerateTrees(random, width, height,
            treePrefab, generatedParent, transform, GridToWorld,
            tile => LandmarkGenerator.IsInClearing(context, tile, altarPosition)
                    || walkablePaths.Contains(tile),
            treeDensity, vegetationNoiseScale, clusteringStrength,
            minimumTreeDensity, maximumTreeDensity, treePositionVariation, treePositions);

        bushCount = VegetationGenerator.GenerateBushes(random, width, height,
            bushPrefabs, generatedParent, transform, GridToWorld,
            tile => LandmarkGenerator.IsInClearing(context, tile, altarPosition)
                    || walkablePaths.Contains(tile),
            position => VegetationClearance.IsBushPositionClear(context, position),
            treePositions, bushDensity, bushNoiseScale, bushClusteringStrength,
            bushPositionVariation, bushScaleRange, bushTreeClearance);
        return treeCount;
    }

    private bool ValidateSettings()
    {
        if (groundTilePrefab == null || treePrefab == null ||
            cavePrefab == null || altarPrefab == null || dirtPathMaterial == null)
        {
            Debug.LogError("Forest generation failed: missing prefab or path material.", this);
            return false;
        }

        if (width < 5 || height < 5 || tileSize <= 0f || pathWidth <= 0f ||
            caveCount < 1 || minimumCaveDistance < 0 ||
            minimumAltarCaveDistance < 0 || clearingRadius < 0 ||
            treePathClearance < 0f || vegetationNoiseScale <= 0f ||
            minimumTreeDensity > maximumTreeDensity ||
            bushScaleRange.x <= 0f || bushScaleRange.y < bushScaleRange.x ||
            bushPositionVariation < 0f || bushTreeClearance < 0f ||
            bushPathClearance < 0f || bushLandmarkClearance < 0f ||
            bushNoiseScale <= 0f)
        {
            Debug.LogError("Forest generation failed: invalid settings.", this);
            return false;
        }

        return true;
    }

    private void ClearGeneratedObjects()
    {
        Transform oldContainer = generatedParent;

        if (oldContainer == null)
            oldContainer = transform.Find("GeneratedForest");

        if (oldContainer != null)
        {
            oldContainer.gameObject.SetActive(false);

            if (Application.isPlaying)
                Destroy(oldContainer.gameObject);
            else
                DestroyImmediate(oldContainer.gameObject);
        }

        GameObject container = new GameObject("GeneratedForest");

        generatedParent = container.transform;
        generatedParent.SetParent(transform, false);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugPaths)
            return;

        Gizmos.color = Color.yellow;

        foreach (List<Vector3> path in generatedPaths)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(
                    transform.TransformPoint(path[i] + Vector3.up * 0.3f),
                    transform.TransformPoint(path[i + 1] + Vector3.up * 0.3f)
                );
            }
        }
    }
}