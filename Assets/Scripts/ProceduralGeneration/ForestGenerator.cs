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

    [Header("Seasonal Assets")]
    [SerializeField] private ForestAssetDatabase assetDatabase;

    [Tooltip("Vertical offset applied to imported trees after ground alignment.")]
    [SerializeField] private float importedTreeHeightOffset = 0f;

    [SerializeField, Range(0f, 1f)]
    private float rockDensity = 0.12f;

    [SerializeField]
    private Vector2 rockScaleRange =
        new Vector2(0.8f, 1.2f);

    private const string NextSeasonKey = "Forest.NextSeasonIndex";

    private static bool hasActiveSession;
    private static int activeSessionSeed;
    private static ForestSeason activeSessionSeason;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionOnLaunch()
    {
        hasActiveSession = false;
        activeSessionSeed = default;
        activeSessionSeason = default;
    }

    private static readonly ForestSeason[] SeasonCycle =
    {
        ForestSeason.Normal,
        ForestSeason.Autumn,
        ForestSeason.Winter
    };

    private ForestSeason currentSeason = ForestSeason.Normal;

    [Header("Path Appearance")]
    [SerializeField] private Material dirtPathMaterial;
    [SerializeField, Min(0.1f)] private float pathWidth = 3f;

    [SerializeField, Range(0f, 0.3f)]
    private float pathWidthVariation = 0.12f;

    [Header("Path Generation")]
    [SerializeField, Range(0f, 0.5f)]
    private float pathCurvature = 0.14f;

    [SerializeField, Range(1, 8)]
    private int intermediateWaypoints = 3;

    [SerializeField, Range(4, 30)]
    private int samplesPerSection = 12;

    [Header("Forest Settings")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 67890;
    [SerializeField] private int currentSeed;

    [SerializeField, Range(0f, 1f)]
    private float treeDensity = 0.45f;

    [Header("Vegetation Generation")]
    [SerializeField, Range(0.02f, 0.5f)]
    private float vegetationNoiseScale = 0.12f;

    [SerializeField, Range(0f, 1f)]
    private float clusteringStrength = 0.65f;

    [SerializeField, Range(0f, 1f)]
    private float minimumTreeDensity = 0.20f;

    [SerializeField, Range(0f, 1f)]
    private float maximumTreeDensity = 0.80f;

    [SerializeField, Range(0f, 1f)]
    private float treePositionVariation = 0.70f;

    [SerializeField, Min(0f)]
    private float treePathClearance = 1.25f;

    [Header("Vegetation Spacing")]
    [SerializeField, Min(0f)] private float treeSpacingBuffer = 0.15f;
    [SerializeField, Range(0.1f, 1f)] private float treeFootprintScale = 0.45f;
    [SerializeField, Min(0f)] private float bushSpacingBuffer = 0.1f;
    [SerializeField, Range(0.1f, 1f)] private float bushFootprintScale = 0.75f;
    [SerializeField, Min(0f)] private float rockSpacingBuffer = 0.1f;
    [SerializeField, Range(0.1f, 1f)] private float rockFootprintScale = 0.85f;

    [Header("Bush Generation")]
    [Tooltip("Legacy bush prefabs, used if no asset database is assigned.")]
    [SerializeField] private GameObject[] bushPrefabs;

    [SerializeField, Range(0f, 1f)]
    private float bushDensity = 0.35f;

    [SerializeField, Range(0.02f, 0.5f)]
    private float bushNoiseScale = 0.16f;

    [SerializeField, Range(0f, 1f)]
    private float bushClusteringStrength = 0.65f;

    [SerializeField, Min(0f)]
    private float bushPositionVariation = 1.0f;

    [SerializeField]
    private Vector2 bushScaleRange =
        new Vector2(0.8f, 1.25f);

    [SerializeField, Min(0f)]
    private float bushTreeClearance = 2.0f;

    [SerializeField, Min(0f)]
    private float bushPathClearance = 1.5f;

    [SerializeField, Min(0f)]
    private float bushLandmarkClearance = 7.5f;

    [Header("Ground Cover Generation")]
    [SerializeField, Range(0f, 1f)]
    private float groundCoverDensity = 0.6f;

    [SerializeField, Range(0.02f, 0.5f)]
    private float groundCoverNoiseScale = 0.15f;

    [SerializeField, Range(0f, 1f)]
    private float groundCoverClusteringStrength = 0.5f;

    [SerializeField, Min(0f)]
    private float groundCoverPositionVariation = 1.5f;

    [SerializeField]
    private Vector2 groundCoverHeightRange =
        new Vector2(0.2f, 0.65f);

    [SerializeField, Min(1)] private int groundCoverInstancesPerTile = 5;
    [SerializeField, Min(0f)] private float groundCoverPathClearance = 0.25f;
    [SerializeField, Min(0f)] private float groundCoverLandmarkClearance = 3f;

    [Header("Cave Settings")]
    [SerializeField, Min(1)] private int caveCount = 3;
    [SerializeField, Min(0)] private int minimumCaveDistance = 7;
    [SerializeField, Min(0)] private int minimumAltarCaveDistance = 9;
    [SerializeField, Min(0)] private int clearingRadius = 1;

    [Header("Debug")]
    [SerializeField] private bool showDebugPaths = true;

    private readonly List<Vector2Int> cavePositions =
        new List<Vector2Int>();

    private readonly List<GameObject> caveObjects =
        new List<GameObject>();

    private readonly HashSet<Vector2Int> walkablePaths =
        new HashSet<Vector2Int>();

    private readonly List<List<Vector3>> generatedPaths =
        new List<List<Vector3>>();

    private Transform generatedParent;

    // Public API for the game director and other systems.
    // References are updated after each generation.
    public IReadOnlyList<Vector2Int> CavePositions =>
        cavePositions;

    public IReadOnlyList<GameObject> CaveObjects =>
        caveObjects;

    public IReadOnlyCollection<Vector2Int> WalkablePaths =>
        walkablePaths;

    public GameObject AltarObject { get; private set; }
    public bool IsGenerated { get; private set; }

    public int CurrentSeed => currentSeed;
    public ForestSeason CurrentSeason => currentSeason;

    public event Action<ForestGenerator> ForestGenerated;

    private void Start()
    {
        GenerateForest();
    }

    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return transform.TransformPoint(
            new Vector3(
                gridPosition.x * tileSize,
                0f,
                gridPosition.y * tileSize)
        );
    }

    // Cave object position, not an interior spawn point.
    public Vector3 GetCaveWorldPosition(int index)
    {
        if (index < 0 || index >= caveObjects.Count)
        {
            Debug.LogError(
                $"Invalid cave index: {index}",
                this);

            return Vector3.zero;
        }

        return caveObjects[index].transform.position;
    }

    public void StartNewGame()
    {
        hasActiveSession = false;
        GenerateForest();
    }

    public void GenerateForest()
    {
        if (!ValidateSettings())
            return;

        int generationSeed = hasActiveSession
            ? activeSessionSeed
            : (useRandomSeed ? Guid.NewGuid().GetHashCode() : seed);

        System.Random random =
            new System.Random(generationSeed);

        int nextSeasonIndex = Mathf.Clamp(
            PlayerPrefs.GetInt(NextSeasonKey, 0),
            0,
            SeasonCycle.Length - 1
        );

        ForestSeason generationSeason = hasActiveSession
            ? activeSessionSeason
            : SeasonCycle[nextSeasonIndex];

        Vector2Int altarPosition =
            new Vector2Int(width / 2, height / 2);

        // Ensure caves can be placed before clearing
        // the previously generated environment.
        ForestGenerationContext context = BuildContext();

        List<Vector2Int> newCaves =
            LandmarkGenerator.GenerateCavePositions(
                context,
                random,
                altarPosition);

        if (newCaves.Count != caveCount)
        {
            Debug.LogError(
                "Forest generation failed: unable to place " +
                "all caves. Check distance constraints.",
                this);

            return;
        }

        IsGenerated = false;

        ClearGeneratedObjects();
        context.generatedParent = generatedParent;

        currentSeed = generationSeed;
        currentSeason = generationSeason;

        cavePositions.Clear();
        cavePositions.AddRange(newCaves);

        caveObjects.Clear();
        walkablePaths.Clear();
        generatedPaths.Clear();

        AltarObject = null;

        PathGenerator.GeneratePaths(
            context,
            random,
            altarPosition);

        int treeCount = GenerateEnvironment(
            context,
            random,
            altarPosition,
            out int bushCount,
            out int rockCount,
            out int groundCoverCount);

        LandmarkGenerator.SpawnLandmarks(
            context,
            altarPosition);

        AltarObject = context.AltarObject;

        IsGenerated = true;

        // Only a new game advances the season; dungeon returns do not.
        if (!hasActiveSession)
        {
            activeSessionSeed = generationSeed;
            activeSessionSeason = generationSeason;
            hasActiveSession = true;

            int followingSeasonIndex =
                (nextSeasonIndex + 1) % SeasonCycle.Length;
            PlayerPrefs.SetInt(NextSeasonKey, followingSeasonIndex);
            PlayerPrefs.Save();
        }

        Debug.Log(
            $"Forest generated. " +
            $"Seed: {CurrentSeed}, " +
            $"Grid: {width}x{height}, " +
            $"Season: {CurrentSeason}, " +
            $"Next Season: {SeasonCycle[PlayerPrefs.GetInt(NextSeasonKey, 0)]}, " +
            $"Trees: {treeCount}, " +
            $"Bushes: {bushCount}, " +
            $"Rocks: {rockCount}, " +
            $"Ground Cover: {groundCoverCount}, " +
            $"Caves: {caveObjects.Count}, " +
            $"Paths: {generatedPaths.Count}",
            this);

        for (int i = 0; i < caveObjects.Count; i++)
        {
            Debug.Log(
                $"{caveObjects[i].name}: " +
                $"{caveObjects[i].transform.position}",
                caveObjects[i]);
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
            minimumAltarCaveDistance =
                minimumAltarCaveDistance,
            clearingRadius = clearingRadius,

            generatedParent = generatedParent,
            forestTransform = transform,
            cavePositions = cavePositions,
            caveObjects = caveObjects,
            walkablePaths = walkablePaths,
            generatedPaths = generatedPaths
        };
    }

    // All systems use the shared seeded random generator.
    private int GenerateEnvironment(
        ForestGenerationContext context,
        System.Random random,
        Vector2Int altarPosition,
        out int bushCount,
        out int rockCount,
        out int groundCoverCount)
    {
        TerrainGenerator.GenerateGround(
            width,
            height,
            groundTilePrefab,
            generatedParent,
            GridToWorld);

        List<Vector3> treePositions =
            new List<Vector3>();

        VegetationSpacing vegetationSpacing = new VegetationSpacing();

        bool useDatabase = assetDatabase != null;

        Func<GameObject> chooseTree = () => useDatabase
            ? assetDatabase.GetRandom(
                ForestAssetType.Tree,
                currentSeason,
                random)
            : treePrefab;

        Func<GameObject> chooseBush = () => useDatabase
            ? assetDatabase.GetRandom(
                ForestAssetType.Bush,
                currentSeason,
                random)
            : ChooseLegacyBush(random);

        int treeCount =
            VegetationGenerator.GenerateTrees(
                random,
                width,
                height,
                chooseTree,
                generatedParent,
                transform,
                GridToWorld,
                tile =>
                    LandmarkGenerator.IsInClearing(
                        context,
                        tile,
                        altarPosition)
                    || walkablePaths.Contains(tile),
                treeDensity,
                vegetationNoiseScale,
                clusteringStrength,
                minimumTreeDensity,
                maximumTreeDensity,
                treePositionVariation,
                treePositions,
                useDatabase
                    ? importedTreeHeightOffset
                    : 2.5f,
                null,
                vegetationSpacing,
                treeSpacingBuffer,
                treeFootprintScale);

        bushCount =
            VegetationGenerator.GenerateBushes(
                random,
                width,
                height,
                chooseBush,
                generatedParent,
                transform,
                GridToWorld,
                tile =>
                    LandmarkGenerator.IsInClearing(
                        context,
                        tile,
                        altarPosition)
                    || walkablePaths.Contains(tile),
                position =>
                    VegetationClearance.IsBushPositionClear(
                        context,
                        position),
                treePositions,
                bushDensity,
                bushNoiseScale,
                bushClusteringStrength,
                bushPositionVariation,
                bushScaleRange,
                bushTreeClearance,
                null,
                vegetationSpacing,
                bushSpacingBuffer,
                bushFootprintScale);

        rockCount = useDatabase
            ? VegetationGenerator.GenerateRocks(
                random,
                width,
                height,
                () => assetDatabase.GetRandom(
                    ForestAssetType.Rock,
                    currentSeason,
                    random),
                generatedParent,
                transform,
                GridToWorld,
                tile =>
                    LandmarkGenerator.IsInClearing(
                        context,
                        tile,
                        altarPosition)
                    || walkablePaths.Contains(tile),
                position =>
                    VegetationClearance.IsBushPositionClear(
                        context,
                        position),
                treePositions,
                rockDensity,
                rockScaleRange,
                bushTreeClearance,
                null,
                vegetationSpacing,
                rockSpacingBuffer,
                rockFootprintScale)
            : 0;

        // Spawn ground cover last to preserve tree, bush and rock placement.
        groundCoverCount = useDatabase &&
            assetDatabase.GetEligibleCount(
                ForestAssetType.GroundCover,
                currentSeason) > 0
            ? VegetationGenerator.GenerateGroundCover(
                random,
                width,
                height,
                () => assetDatabase.GetRandom(
                    ForestAssetType.GroundCover,
                    currentSeason,
                    random),
                generatedParent,
                transform,
                GridToWorld,
                tile =>
                    LandmarkGenerator.IsInClearing(
                        context,
                        tile,
                        altarPosition)
                    || walkablePaths.Contains(tile),
                position =>
                    VegetationClearance.IsGroundCoverPositionClear(
                        context,
                        position,
                        groundCoverPathClearance,
                        groundCoverLandmarkClearance),
                groundCoverDensity,
                groundCoverNoiseScale,
                groundCoverClusteringStrength,
                groundCoverPositionVariation,
                groundCoverHeightRange,
                groundCoverInstancesPerTile)
            : 0;

        return treeCount;
    }

    private GameObject ChooseLegacyBush(
        System.Random random)
    {
        if (bushPrefabs == null ||
            bushPrefabs.Length == 0)
        {
            return null;
        }

        int count = 0;

        foreach (GameObject prefab in bushPrefabs)
        {
            if (prefab != null)
                count++;
        }

        if (count == 0)
            return null;

        int index = random.Next(count);

        foreach (GameObject prefab in bushPrefabs)
        {
            if (prefab == null)
                continue;

            if (index-- == 0)
                return prefab;
        }

        return null;
    }

    private bool ValidateSettings()
    {
        if (groundTilePrefab == null ||
            cavePrefab == null ||
            altarPrefab == null ||
            dirtPathMaterial == null ||
            (assetDatabase == null && treePrefab == null))
        {
            Debug.LogError(
                "Forest generation failed: " +
                "missing prefab or path material.",
                this);

            return false;
        }

        if (width < 5 ||
            height < 5 ||
            tileSize <= 0f ||
            pathWidth <= 0f ||
            caveCount < 1 ||
            minimumCaveDistance < 0 ||
            minimumAltarCaveDistance < 0 ||
            clearingRadius < 0 ||
            treePathClearance < 0f ||
            vegetationNoiseScale <= 0f ||
            minimumTreeDensity > maximumTreeDensity ||
            bushScaleRange.x <= 0f ||
            bushScaleRange.y < bushScaleRange.x ||
            bushPositionVariation < 0f ||
            bushTreeClearance < 0f ||
            bushPathClearance < 0f ||
            bushLandmarkClearance < 0f ||
            bushNoiseScale <= 0f ||
            rockDensity < 0f ||
            rockScaleRange.x <= 0f ||
            rockScaleRange.y < rockScaleRange.x ||
            groundCoverNoiseScale <= 0f ||
            groundCoverPositionVariation < 0f ||
            groundCoverHeightRange.x <= 0f ||
            groundCoverHeightRange.y < groundCoverHeightRange.x ||
            groundCoverInstancesPerTile < 1 ||
            groundCoverPathClearance < 0f ||
            groundCoverLandmarkClearance < 0f ||
            treeSpacingBuffer < 0f ||
            bushSpacingBuffer < 0f ||
            rockSpacingBuffer < 0f ||
            treeFootprintScale <= 0f ||
            bushFootprintScale <= 0f ||
            rockFootprintScale <= 0f)
        {
            Debug.LogError(
                "Forest generation failed: invalid settings.",
                this);

            return false;
        }

        // All three seasons must have usable tree assets.
        if (assetDatabase != null)
        {
            foreach (ForestSeason season in SeasonCycle)
            {
                if (assetDatabase.GetEligibleCount(
                        ForestAssetType.Tree,
                        season) == 0)
                {
                    Debug.LogError(
                        $"Forest asset database has no " +
                        $"compatible trees for {season}.",
                        this);

                    return false;
                }
            }
        }

        return true;
    }

    private void ClearGeneratedObjects()
    {
        Transform oldContainer = generatedParent;

        if (oldContainer == null)
        {
            oldContainer =
                transform.Find("GeneratedForest");
        }

        if (oldContainer != null)
        {
            oldContainer.gameObject.SetActive(false);

            if (Application.isPlaying)
                Destroy(oldContainer.gameObject);
            else
                DestroyImmediate(oldContainer.gameObject);
        }

        GameObject container =
            new GameObject("GeneratedForest");

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
                    transform.TransformPoint(
                        path[i] + Vector3.up * 0.3f),
                    transform.TransformPoint(
                        path[i + 1] + Vector3.up * 0.3f));
            }
        }
    }
}