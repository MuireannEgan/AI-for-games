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

    public Vector3 GetCaveWorldPosition(int index)
    {
        if (index < 0 || index >= caveObjects.Count)
        {
            Debug.LogError($"Invalid cave index: {index}", this);
            return Vector3.zero;
        }

        return caveObjects[index].transform.position;
    }

    public void GenerateForest()
    {
        if (!ValidateSettings())
            return;

        int generationSeed = useRandomSeed
            ? Guid.NewGuid().GetHashCode()
            : seed;

        System.Random random = new System.Random(generationSeed);
        Vector2Int altarPosition = new Vector2Int(width / 2, height / 2);

        // Check cave placement before removing the existing forest.
        List<Vector2Int> newCaves = GenerateCavePositions(random, altarPosition);

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

        currentSeed = generationSeed;

        cavePositions.Clear();
        cavePositions.AddRange(newCaves);

        caveObjects.Clear();
        walkablePaths.Clear();
        generatedPaths.Clear();
        AltarObject = null;

        GeneratePaths(random, altarPosition);
        int treeCount = GenerateEnvironment(random, altarPosition);
        SpawnLandmarks(altarPosition);

        IsGenerated = true;

        Debug.Log(
            $"Forest generated. Seed: {CurrentSeed}, Grid: {width}x{height}, " +
            $"Trees: {treeCount}, Caves: {caveObjects.Count}, Paths: {generatedPaths.Count}",
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
            minimumTreeDensity > maximumTreeDensity)
        {
            Debug.LogError("Forest generation failed: invalid settings.", this);
            return false;
        }

        return true;
    }

    private List<Vector2Int> GenerateCavePositions(
        System.Random random,
        Vector2Int altarPosition)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        List<Vector2Int> candidates = new List<Vector2Int>();

        int margin = Mathf.Max(1, clearingRadius);

        for (int x = margin; x < width - margin; x++)
        {
            for (int z = margin; z < height - margin; z++)
            {
                Vector2Int candidate = new Vector2Int(x, z);
                float distanceFromAltar = Vector2Int.Distance(candidate, altarPosition);

                if (distanceFromAltar >= minimumAltarCaveDistance)
                    candidates.Add(candidate);
            }
        }

        // Fisher-Yates shuffle to randomise cave candidates.
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
                if (Vector2Int.Distance(candidate, existing) < minimumCaveDistance)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
                continue;

            result.Add(candidate);

            if (result.Count == caveCount)
                break;
        }

        return result;
    }

    private void GeneratePaths(System.Random random, Vector2Int altarPosition)
    {
        foreach (Vector2Int cave in cavePositions)
        {
            List<Vector3> path = GenerateWindingPath(altarPosition, cave, random);

            generatedPaths.Add(path);
            ReservePathTiles(path);
            CreatePathMesh(path, random);
        }
    }

    private List<Vector3> GenerateWindingPath(
        Vector2Int start,
        Vector2Int end,
        System.Random random)
    {
        Vector3 startPoint = new Vector3(start.x * tileSize, 0f, start.y * tileSize);
        Vector3 endPoint = new Vector3(end.x * tileSize, 0f, end.y * tileSize);

        Vector3 direction = endPoint - startPoint;
        float totalDistance = direction.magnitude;

        Vector3 perpendicular = new Vector3(-direction.z, 0f, direction.x).normalized;

        List<Vector3> waypoints = new List<Vector3> { startPoint };

        for (int i = 1; i <= intermediateWaypoints; i++)
        {
            float t = (float)i / (intermediateWaypoints + 1);
            Vector3 basePoint = Vector3.Lerp(startPoint, endPoint, t);

            float randomOffset = (float)random.NextDouble() * 2f - 1f;

            float maximumBend = Mathf.Min(
                totalDistance * pathCurvature,
                totalDistance / (intermediateWaypoints + 1) * 0.65f
            );

            float envelope = Mathf.Sin(t * Mathf.PI);

            Vector3 waypoint = basePoint +
                perpendicular * randomOffset * maximumBend * envelope;

            waypoints.Add(ClampToMap(waypoint));
        }

        waypoints.Add(endPoint);

        // Smooth the waypoints into a continuous path using Catmull-Rom interpolation.
        List<Vector3> smoothPath = new List<Vector3>();

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 p0 = waypoints[Mathf.Max(i - 1, 0)];
            Vector3 p1 = waypoints[i];
            Vector3 p2 = waypoints[i + 1];
            Vector3 p3 = waypoints[Mathf.Min(i + 2, waypoints.Count - 1)];

            for (int j = 0; j < samplesPerSection; j++)
            {
                float t = (float)j / samplesPerSection;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);

                smoothPath.Add(ClampToMap(point));
            }
        }

        smoothPath.Add(endPoint);
        return smoothPath;
    }

    private Vector3 CatmullRom(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private Vector3 ClampToMap(Vector3 point)
    {
        float margin = pathWidth * (1f + pathWidthVariation) * 0.5f;

        return new Vector3(
            Mathf.Clamp(point.x, margin, (width - 1) * tileSize - margin),
            0f,
            Mathf.Clamp(point.z, margin, (height - 1) * tileSize - margin)
        );
    }

    private void ReservePathTiles(List<Vector3> path)
    {
        float maximumHalfWidth = pathWidth * (1f + pathWidthVariation) * 0.5f;

        float clearance = maximumHalfWidth +
            treePathClearance +
            treePositionVariation * 1.4143f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int tile = new Vector2Int(x, z);
                Vector3 centre = new Vector3(x * tileSize, 0f, z * tileSize);

                for (int i = 0; i < path.Count - 1; i++)
                {
                    float distance = DistanceToSegment(centre, path[i], path[i + 1]);

                    if (distance <= clearance)
                    {
                        walkablePaths.Add(tile);
                        break;
                    }
                }
            }
        }
    }

    private float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;

        if (lengthSquared < 0.0001f)
            return Vector3.Distance(point, start);

        float t = Vector3.Dot(point - start, segment) / lengthSquared;
        t = Mathf.Clamp01(t);

        Vector3 closestPoint = start + segment * t;
        return Vector3.Distance(point, closestPoint);
    }

    private void CreatePathMesh(List<Vector3> path, System.Random random)
    {
        GameObject pathObject = new GameObject("WindingDirtPath");
        pathObject.transform.SetParent(generatedParent, false);

        MeshFilter meshFilter = pathObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = pathObject.AddComponent<MeshRenderer>();

        int pointCount = path.Count;

        Vector3[] vertices = new Vector3[pointCount * 2];
        Vector2[] uvs = new Vector2[pointCount * 2];
        int[] triangles = new int[(pointCount - 1) * 6];

        float widthPhase = (float)random.NextDouble() * Mathf.PI * 2f;
        float accumulatedDistance = 0f;

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 tangent;

            if (i == 0)
                tangent = path[1] - path[0];
            else if (i == pointCount - 1)
                tangent = path[i] - path[i - 1];
            else
                tangent = path[i + 1] - path[i - 1];

            tangent.Normalize();

            Vector3 side = new Vector3(-tangent.z, 0f, tangent.x);
            float progress = (float)i / (pointCount - 1);

            float widthNoise = Mathf.Sin(progress * Mathf.PI * 4f + widthPhase);
            float currentWidth = pathWidth * (1f + widthNoise * pathWidthVariation);

            Vector3 centre = path[i] + Vector3.up * 0.06f;

            vertices[i * 2] = centre - side * currentWidth * 0.5f;
            vertices[i * 2 + 1] = centre + side * currentWidth * 0.5f;

            if (i > 0)
                accumulatedDistance += Vector3.Distance(path[i - 1], path[i]);

            uvs[i * 2] = new Vector2(0f, accumulatedDistance / pathWidth);
            uvs[i * 2 + 1] = new Vector2(1f, accumulatedDistance / pathWidth);
        }

        // Two triangles per path segment.
        for (int i = 0; i < pointCount - 1; i++)
        {
            int vertex = i * 2;
            int triangle = i * 6;

            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 1;
            triangles[triangle + 2] = vertex + 2;

            triangles[triangle + 3] = vertex + 1;
            triangles[triangle + 4] = vertex + 3;
            triangles[triangle + 5] = vertex + 2;
        }

        Mesh mesh = new Mesh
        {
            name = "ProceduralWindingPath",
            vertices = vertices,
            uv = uvs,
            triangles = triangles
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = dirtPathMaterial;
    }

    private int GenerateEnvironment(System.Random random, Vector2Int altarPosition)
    {
        int treeCount = 0;

        float noiseOffsetX = (float)random.NextDouble() * 10000f;
        float noiseOffsetZ = (float)random.NextDouble() * 10000f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int tile = new Vector2Int(x, z);
                Vector3 worldPosition = GridToWorld(tile);

                Instantiate(
                    groundTilePrefab,
                    worldPosition,
                    Quaternion.identity,
                    generatedParent
                );

                if (IsInClearing(tile, altarPosition) || walkablePaths.Contains(tile))
                    continue;

                // Vary the tree probability using Perlin noise.
                float noiseValue = Mathf.PerlinNoise(
                    x * vegetationNoiseScale + noiseOffsetX,
                    z * vegetationNoiseScale + noiseOffsetZ
                );

                float densityVariation = (noiseValue - 0.5f) *
                    2f * clusteringStrength;

                float localDensity = Mathf.Clamp(
                    treeDensity + densityVariation,
                    minimumTreeDensity,
                    maximumTreeDensity
                );

                if (random.NextDouble() >= localDensity)
                    continue;

                float offsetX = ((float)random.NextDouble() * 2f - 1f) *
                    treePositionVariation;

                float offsetZ = ((float)random.NextDouble() * 2f - 1f) *
                    treePositionVariation;

                Vector3 offset = new Vector3(offsetX, 0f, offsetZ);

                Vector3 treePosition = worldPosition +
                    transform.TransformVector(offset) +
                    Vector3.up * 2.5f;

                GameObject tree = Instantiate(
                    treePrefab,
                    treePosition,
                    Quaternion.identity,
                    generatedParent
                );

                tree.name = $"Tree_{x}_{z}";
                treeCount++;
            }
        }

        return treeCount;
    }

    private void SpawnLandmarks(Vector2Int altarPosition)
    {
        for (int i = 0; i < cavePositions.Count; i++)
        {
            Vector3 position = GridToWorld(cavePositions[i]) + Vector3.up * 1.5f;

            GameObject cave = Instantiate(
                cavePrefab,
                position,
                Quaternion.identity,
                generatedParent
            );

            cave.name = $"Cave_{i + 1}";
            caveObjects.Add(cave);
        }

        Vector3 altarWorldPosition = GridToWorld(altarPosition) + Vector3.up * 0.5f;

        AltarObject = Instantiate(
            altarPrefab,
            altarWorldPosition,
            Quaternion.identity,
            generatedParent
        );

        AltarObject.name = "Forest_Altar";
    }

    private bool IsInClearing(Vector2Int tile, Vector2Int altarPosition)
    {
        if (Vector2Int.Distance(tile, altarPosition) <= clearingRadius)
            return true;

        foreach (Vector2Int cave in cavePositions)
        {
            if (Vector2Int.Distance(tile, cave) <= clearingRadius)
                return true;
        }

        return false;
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
