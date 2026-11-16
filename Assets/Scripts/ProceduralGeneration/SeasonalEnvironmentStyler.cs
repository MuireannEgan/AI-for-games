using System;
using UnityEngine;

// Applies seasonal ground/path materials after forest generation.
// Also adds low-poly snow patches to winter paths.
[RequireComponent(typeof(ForestGenerator))]
public class SeasonalEnvironmentStyler : MonoBehaviour
{
    [Header("Ground Tile")]
    [SerializeField] private GameObject groundTilePrefab;

    [Header("Normal Materials")]
    [SerializeField] private Material normalGround;
    [SerializeField] private Material normalPath;

    [Header("Autumn Materials")]
    [SerializeField] private Material autumnGround;
    [SerializeField] private Material autumnPath;

    [Header("Winter Materials")]
    [SerializeField] private Material winterGround;
    [SerializeField] private Material winterPath;
    [SerializeField] private Material snowPatchMaterial;

    [Header("Winter Snow Patches")]
    [SerializeField, Range(0f, 1f)]
    private float snowCoverage = 0.45f;

    [SerializeField, Min(0.2f)]
    private float patchSpacing = 2.5f;

    [SerializeField]
    private Vector2 patchLengthRange =
        new Vector2(0.5f, 1.6f);

    [SerializeField, Range(0.1f, 0.8f)]
    private float maximumPatchWidth = 0.55f;

    private ForestGenerator forest;

    private void Awake()
    {
        forest = GetComponent<ForestGenerator>();
    }

    private void OnEnable()
    {
        if (forest == null)
            forest = GetComponent<ForestGenerator>();

        forest.ForestGenerated += ApplySeason;
    }

    private void OnDisable()
    {
        if (forest != null)
            forest.ForestGenerated -= ApplySeason;
    }

    private void ApplySeason(ForestGenerator generator)
    {
        if (!generator.IsGenerated)
            return;

        Transform generated =
            generator.transform.Find("GeneratedForest");

        if (generated == null)
        {
            Debug.LogWarning(
                "GeneratedForest container not found.",
                this);

            return;
        }

        ForestSeason season = generator.CurrentSeason;

        Material ground = GetGroundMaterial(season);
        Material path = GetPathMaterial(season);

        int groundCount = 0;
        int pathCount = 0;

        int pathIndex = 0;

        foreach (Transform child in generated)
        {
            if (IsGroundTile(child.gameObject))
            {
                if (ground != null)
                    ApplyMaterial(child.gameObject, ground);

                groundCount++;
            }
            else if (child.name == "WindingDirtPath")
            {
                MeshRenderer renderer =
                    child.GetComponent<MeshRenderer>();

                if (renderer != null && path != null)
                    renderer.sharedMaterial = path;

                if (season == ForestSeason.Winter &&
                    snowPatchMaterial != null)
                {
                    CreateSnowPatches(
                        child.gameObject,
                        generator.CurrentSeed,
                        pathIndex);
                }

                pathIndex++;
                pathCount++;
            }
        }

        Debug.Log(
            $"Seasonal environment applied: {season}. " +
            $"Ground tiles: {groundCount}, " +
            $"Paths: {pathCount}",
            this);
    }

    private bool IsGroundTile(GameObject obj)
    {
        if (groundTilePrefab == null)
            return false;

        // TerrainGenerator currently instantiates the prefab
        // without renaming it, producing "PrefabName(Clone)".
        return obj.name ==
            groundTilePrefab.name + "(Clone)";
    }

    private Material GetGroundMaterial(ForestSeason season)
    {
        switch (season)
        {
            case ForestSeason.Autumn:
                return autumnGround;

            case ForestSeason.Winter:
                return winterGround;

            default:
                return normalGround;
        }
    }

    private Material GetPathMaterial(ForestSeason season)
    {
        switch (season)
        {
            case ForestSeason.Autumn:
                return autumnPath;

            case ForestSeason.Winter:
                return winterPath;

            default:
                return normalPath;
        }
    }

    private static void ApplyMaterial(
        GameObject obj,
        Material material)
    {
        Renderer[] renderers =
            obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            // Replace the renderer's materials on this
            // instance without modifying the source asset.
            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;

            renderer.sharedMaterials = materials;
        }
    }

    private void CreateSnowPatches(
        GameObject pathObject,
        int seed,
        int pathIndex)
    {
        MeshFilter filter =
            pathObject.GetComponent<MeshFilter>();

        if (filter == null || filter.sharedMesh == null)
            return;

        Mesh pathMesh = filter.sharedMesh;
        Vector3[] pathVertices = pathMesh.vertices;

        int pointCount = pathVertices.Length / 2;

        if (pointCount < 2)
            return;

        // Use a separate RNG so decorating the path
        // cannot alter procedural forest generation.
        System.Random random = new System.Random(
            unchecked(seed ^ (pathIndex + 1) * 7919));

        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        float travelled = 0f;
        float nextPatchDistance = patchSpacing;

        for (int i = 1; i < pointCount; i++)
        {
            Vector3 previous = GetCentre(pathVertices, i - 1);
            Vector3 current = GetCentre(pathVertices, i);

            float segmentLength =
                Vector3.Distance(previous, current);

            if (segmentLength < 0.0001f)
                continue;

            float distanceBefore = travelled;
            travelled += segmentLength;

            while (nextPatchDistance <= travelled)
            {
                float t = Mathf.Clamp01(
                    (nextPatchDistance - distanceBefore) /
                    segmentLength);

                if (random.NextDouble() < snowCoverage)
                {
                    Vector3 centre =
                        Vector3.Lerp(previous, current, t);

                    Vector3 tangent =
                        (current - previous).normalized;

                    Vector3 side = new Vector3(
                        -tangent.z,
                        0f,
                        tangent.x);

                    // Calculate the actual path width at
                    // the current point.
                    Vector3 left = Vector3.Lerp(
                        pathVertices[(i - 1) * 2],
                        pathVertices[i * 2],
                        t);

                    Vector3 right = Vector3.Lerp(
                        pathVertices[(i - 1) * 2 + 1],
                        pathVertices[i * 2 + 1],
                        t);

                    float halfWidth =
                        Vector3.Distance(left, right) * 0.5f;

                    float offset =
                        ((float)random.NextDouble() * 2f - 1f)
                        * halfWidth * 0.45f;

                    centre += side * offset;

                    float patchLength = Mathf.Lerp(
                        patchLengthRange.x,
                        patchLengthRange.y,
                        (float)random.NextDouble());

                    float patchWidth =
                        halfWidth * maximumPatchWidth *
                        Mathf.Lerp(
                            0.4f,
                            1f,
                            (float)random.NextDouble());

                    AddPatch(
                        vertices,
                        triangles,
                        centre,
                        tangent,
                        side,
                        patchLength,
                        patchWidth,
                        random);
                }

                nextPatchDistance +=
                    patchSpacing * Mathf.Lerp(
                        0.7f,
                        1.4f,
                        (float)random.NextDouble());
            }
        }

        if (vertices.Count == 0)
            return;

        Mesh snowMesh = new Mesh();
        snowMesh.name = "ProceduralSnowPatches";
        snowMesh.SetVertices(vertices);
        snowMesh.SetTriangles(triangles, 0);
        snowMesh.RecalculateNormals();
        snowMesh.RecalculateBounds();

        GameObject snowObject =
            new GameObject("WinterSnowPatches");

        snowObject.transform.SetParent(
            pathObject.transform,
            false);

        MeshFilter snowFilter =
            snowObject.AddComponent<MeshFilter>();

        MeshRenderer snowRenderer =
            snowObject.AddComponent<MeshRenderer>();

        snowFilter.sharedMesh = snowMesh;
        snowRenderer.sharedMaterial = snowPatchMaterial;
    }

    private static Vector3 GetCentre(
        Vector3[] vertices,
        int index)
    {
        return (
            vertices[index * 2] +
            vertices[index * 2 + 1]
        ) * 0.5f;
    }

    private static void AddPatch(
        System.Collections.Generic.List<Vector3> vertices,
        System.Collections.Generic.List<int> triangles,
        Vector3 centre,
        Vector3 forward,
        Vector3 side,
        float length,
        float width,
        System.Random random)
    {
        const int segments = 8;

        int start = vertices.Count;

        // Slightly above the generated path to avoid
        // z-fighting with the underlying material.
        centre += Vector3.up * 0.025f;

        // Centre vertex.
        vertices.Add(centre);

        for (int i = 0; i < segments; i++)
        {
            float angle =
                i * Mathf.PI * 2f / segments;

            float variation = Mathf.Lerp(
                0.7f,
                1.15f,
                (float)random.NextDouble());

            Vector3 offset =
                forward * Mathf.Cos(angle) *
                    length * 0.5f * variation +
                side * Mathf.Sin(angle) *
                    width * variation;

            vertices.Add(centre + offset);
        }

        // Match the original path's upward-facing winding.
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            triangles.Add(start);
            triangles.Add(start + next + 1);
            triangles.Add(start + i + 1);
        }
    }
}