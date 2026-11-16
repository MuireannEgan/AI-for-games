using System;
using System.Collections.Generic;
using UnityEngine;

// Procedural vegetation placement using noise-based clustering,
// seasonal prefab selection, automatic scaling and ground alignment.
public static class VegetationGenerator
{
    // Adjusts the visible height of an imported model.
    // Uses renderer bounds rather than the FBX's transform scale.
    private static void ScaleToHeight(
        GameObject instance,
        Vector2 heightRange,
        System.Random random)
    {
        if (!TryGetBounds(instance, out Bounds bounds))
            return;

        if (bounds.size.y <= 0.001f)
            return;

        float minimum = Mathf.Max(0.01f, heightRange.x);
        float maximum = Mathf.Max(minimum, heightRange.y);

        float targetHeight = Mathf.Lerp(
            minimum,
            maximum,
            (float)random.NextDouble());

        float multiplier = targetHeight / bounds.size.y;

        // Avoid extreme scaling for unusual FBX models.
        multiplier = Mathf.Clamp(multiplier, 0.1f, 10f);

        instance.transform.localScale *= multiplier;
    }

    // Moves the lowest visible part of an object
    // to the specified world-space ground height.
    private static void AlignToGround(
        GameObject instance,
        float groundY)
    {
        if (!TryGetBounds(instance, out Bounds bounds))
            return;

        float adjustment = groundY - bounds.min.y;

        instance.transform.position +=
            Vector3.up * adjustment;
    }

    private static bool TryGetBounds(
        GameObject instance,
        out Bounds bounds)
    {
        Renderer[] renderers =
            instance.GetComponentsInChildren<Renderer>();

        bounds = new Bounds();
        bool found = false;

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private static bool HasSolidCollider(GameObject instance)
    {
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>())
        {
            if (collider.enabled && !collider.isTrigger)
                return true;
        }

        return false;
    }

    private static void AddVegetationCollider(GameObject instance, ForestAssetType type)
    {
        if (instance == null || HasSolidCollider(instance))
            return;

        if (type != ForestAssetType.Tree &&
            type != ForestAssetType.Bush &&
            type != ForestAssetType.Rock)
            return;

        foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null || !filter.gameObject.activeInHierarchy)
                continue;

            if (filter.GetComponent<Collider>() != null)
                continue;

            MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
        }
    }

    // Uses the FBX's existing rotation rather than
    // Quaternion.identity, which made the models sideways.
    private static GameObject SpawnVegetation(
        GameObject prefab,
        Vector3 position,
        Transform generatedParent,
        System.Random random,
        Vector2 heightRange,
        float groundOffset = 0f)
    {
        if (prefab == null)
            return null;

        GameObject instance = UnityEngine.Object.Instantiate(
            prefab,
            generatedParent);

        instance.transform.position = position;

        // Preserve the imported orientation, then rotate
        // around the WORLD vertical axis.
        float randomYaw =
            (float)random.NextDouble() * 360f;

        instance.transform.rotation =
            Quaternion.AngleAxis(
                randomYaw,
                Vector3.up) * instance.transform.rotation;

        ScaleToHeight(instance, heightRange, random);

        AlignToGround(
            instance,
            position.y + groundOffset);

        return instance;
    }

    private static bool AcceptPlacement(
        GameObject instance,
        VegetationSpacing spacing,
        float buffer,
        float footprintScale)
    {
        if (instance == null)
            return false;

        if (spacing == null || spacing.TryRegister(instance, buffer, footprintScale))
            return true;

        instance.SetActive(false);

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(instance);
        else
            UnityEngine.Object.DestroyImmediate(instance);

        return false;
    }

    // TREES

    public static int GenerateTrees(
        System.Random random,
        int width,
        int height,
        Func<GameObject> chooseTree,
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
        List<Vector3> treePositions = null,
        float treeHeightOffset = 0f,
        Vector2? treeHeightRange = null,
        VegetationSpacing spacing = null,
        float spacingBuffer = 0f,
        float footprintScale = 1f)
    {
        int treeCount = 0;

        Vector2 heights = treeHeightRange ??
            new Vector2(5f, 9f);

        float noiseOffsetX =
            (float)random.NextDouble() * 10000f;

        float noiseOffsetZ =
            (float)random.NextDouble() * 10000f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int tile = new Vector2Int(x, z);

                if (isReserved(tile))
                    continue;

                float noiseValue = Mathf.PerlinNoise(
                    x * vegetationNoiseScale + noiseOffsetX,
                    z * vegetationNoiseScale + noiseOffsetZ);

                float densityVariation =
                    (noiseValue - 0.5f) *
                    2f * clusteringStrength;

                float localDensity = Mathf.Clamp(
                    treeDensity + densityVariation,
                    minimumTreeDensity,
                    maximumTreeDensity);

                if (random.NextDouble() >= localDensity)
                    continue;

                float offsetX =
                    ((float)random.NextDouble() * 2f - 1f) *
                    treePositionVariation;

                float offsetZ =
                    ((float)random.NextDouble() * 2f - 1f) *
                    treePositionVariation;

                Vector3 offset =
                    new Vector3(offsetX, 0f, offsetZ);

                Vector3 position =
                    gridToWorld(tile) +
                    forestTransform.TransformVector(offset);

                GameObject prefab = chooseTree?.Invoke();

                if (prefab == null)
                    continue;

                GameObject tree = SpawnVegetation(
                    prefab,
                    position,
                    generatedParent,
                    random,
                    heights,
                    treeHeightOffset);

                if (!AcceptPlacement(tree, spacing, spacingBuffer, footprintScale))
                    continue;

                AddVegetationCollider(tree, ForestAssetType.Tree);
                tree.name = $"Tree_{x}_{z}";

                // Store actual final position for bush clearance.
                if (treePositions != null)
                    treePositions.Add(
                        tree.transform.position);

                treeCount++;
            }
        }

        return treeCount;
    }

    // BUSHES

    public static int GenerateBushes(
        System.Random random,
        int width,
        int height,
        Func<GameObject> chooseBush,
        Transform generatedParent,
        Transform forestTransform,
        Func<Vector2Int, Vector3> gridToWorld,
        Func<Vector2Int, bool> isReserved,
        Func<Vector3, bool> isPositionClear,
        IReadOnlyList<Vector3> treePositions,
        float density,
        float noiseScale,
        float clusteringStrength,
        float positionVariation,
        Vector2 scaleRange,
        float treeClearance,
        Vector2? bushHeightRange = null,
        VegetationSpacing spacing = null,
        float spacingBuffer = 0f,
        float footprintScale = 1f)
    {
        if (chooseBush == null || density <= 0f)
            return 0;

        int bushCount = 0;

        Vector2 heights = bushHeightRange ??
            new Vector2(1f, 2.4f);

        float noiseOffsetX =
            (float)random.NextDouble() * 10000f;

        float noiseOffsetZ =
            (float)random.NextDouble() * 10000f;

        float minimumDistanceSq =
            treeClearance * treeClearance;

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
                    density +
                    (noise - 0.5f) *
                    2f * clusteringStrength);

                if (random.NextDouble() >= localDensity)
                    continue;

                Vector3 offset = new Vector3(
                    ((float)random.NextDouble() * 2f - 1f) *
                        positionVariation,
                    0f,
                    ((float)random.NextDouble() * 2f - 1f) *
                        positionVariation);

                Vector3 position =
                    gridToWorld(tile) +
                    forestTransform.TransformVector(offset);

                if (!isPositionClear(position))
                    continue;

                bool tooCloseToTree = false;

                if (treePositions != null)
                {
                    foreach (Vector3 treePosition
                             in treePositions)
                    {
                        Vector2 delta = new Vector2(
                            position.x - treePosition.x,
                            position.z - treePosition.z);

                        if (delta.sqrMagnitude <
                            minimumDistanceSq)
                        {
                            tooCloseToTree = true;
                            break;
                        }
                    }
                }

                if (tooCloseToTree)
                    continue;

                GameObject prefab = chooseBush();

                if (prefab == null)
                    continue;

                GameObject bush = SpawnVegetation(
                    prefab,
                    position,
                    generatedParent,
                    random,
                    heights);

                if (bush == null)
                    continue;

                // Keep existing Inspector scale variation.
                float variation = Mathf.Lerp(
                    scaleRange.x,
                    scaleRange.y,
                    (float)random.NextDouble());

                bush.transform.localScale *= variation;

                // Scaling can move the mesh bottom.
                AlignToGround(bush, position.y);

                if (!AcceptPlacement(bush, spacing, spacingBuffer, footprintScale))
                    continue;

                AddVegetationCollider(bush, ForestAssetType.Bush);
                bush.name = $"Bush_{x}_{z}";
                bushCount++;
            }
        }

        return bushCount;
    }

    // ROCKS

    public static int GenerateRocks(
        System.Random random,
        int width,
        int height,
        Func<GameObject> chooseRock,
        Transform generatedParent,
        Transform forestTransform,
        Func<Vector2Int, Vector3> gridToWorld,
        Func<Vector2Int, bool> isReserved,
        Func<Vector3, bool> isPositionClear,
        IReadOnlyList<Vector3> treePositions,
        float density,
        Vector2 scaleRange,
        float treeClearance,
        Vector2? rockHeightRange = null,
        VegetationSpacing spacing = null,
        float spacingBuffer = 0f,
        float footprintScale = 1f)
    {
        if (chooseRock == null || density <= 0f)
            return 0;

        int count = 0;

        Vector2 heights = rockHeightRange ??
            new Vector2(0.6f, 1.8f);

        float minimumDistanceSq =
            treeClearance * treeClearance;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int tile = new Vector2Int(x, z);

                if (isReserved(tile) ||
                    random.NextDouble() >= density)
                    continue;

                Vector3 offset = new Vector3(
                    ((float)random.NextDouble() - 0.5f) *
                        1.5f,
                    0f,
                    ((float)random.NextDouble() - 0.5f) *
                        1.5f);

                Vector3 position =
                    gridToWorld(tile) +
                    forestTransform.TransformVector(offset);

                if (!isPositionClear(position))
                    continue;

                bool tooCloseToTree = false;

                if (treePositions != null)
                {
                    foreach (Vector3 treePosition
                             in treePositions)
                    {
                        Vector2 delta = new Vector2(
                            position.x - treePosition.x,
                            position.z - treePosition.z);

                        if (delta.sqrMagnitude <
                            minimumDistanceSq)
                        {
                            tooCloseToTree = true;
                            break;
                        }
                    }
                }

                if (tooCloseToTree)
                    continue;

                GameObject prefab = chooseRock();

                if (prefab == null)
                    continue;

                GameObject rock = SpawnVegetation(
                    prefab,
                    position,
                    generatedParent,
                    random,
                    heights);

                if (rock == null)
                    continue;

                float variation = Mathf.Lerp(
                    scaleRange.x,
                    scaleRange.y,
                    (float)random.NextDouble());

                rock.transform.localScale *= variation;

                AlignToGround(rock, position.y);

                if (!AcceptPlacement(rock, spacing, spacingBuffer, footprintScale))
                    continue;

                AddVegetationCollider(rock, ForestAssetType.Rock);
                rock.name = $"Rock_{x}_{z}";
                count++;
            }
        }

        return count;
    }

    // GROUND COVER (GRASS, FLOWERS AND SMALL PLANTS)

    public static int GenerateGroundCover(
        System.Random random,
        int width,
        int height,
        Func<GameObject> chooseGroundCover,
        Transform generatedParent,
        Transform forestTransform,
        Func<Vector2Int, Vector3> gridToWorld,
        Func<Vector2Int, bool> isReserved,
        Func<Vector3, bool> isPositionClear,
        float density,
        float noiseScale,
        float clusteringStrength,
        float positionVariation,
        Vector2 heightRange,
        int instancesPerTile = 5)
    {
        if (chooseGroundCover == null || density <= 0f || instancesPerTile <= 0)
            return 0;

        int count = 0;

        float noiseOffsetX =
            (float)random.NextDouble() * 10000f;

        float noiseOffsetZ =
            (float)random.NextDouble() * 10000f;

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
                    density +
                    (noise - 0.5f) *
                    2f * clusteringStrength);

                for (int attempt = 0; attempt < instancesPerTile; attempt++)
                {
                    if (random.NextDouble() >= localDensity)
                        continue;

                    Vector3 offset = new Vector3(
                        ((float)random.NextDouble() * 2f - 1f) *
                            positionVariation,
                        0f,
                        ((float)random.NextDouble() * 2f - 1f) *
                            positionVariation);

                    Vector3 position =
                        gridToWorld(tile) +
                        forestTransform.TransformVector(offset);

                    if (isPositionClear != null &&
                        !isPositionClear(position))
                        continue;

                    GameObject prefab = chooseGroundCover();

                    if (prefab == null)
                        continue;

                    GameObject groundCover = SpawnVegetation(
                        prefab,
                        position,
                        generatedParent,
                        random,
                        heightRange);

                    if (groundCover == null)
                        continue;

                    foreach (Collider collider in
                             groundCover.GetComponentsInChildren<Collider>())
                    {
                        collider.enabled = false;
                    }

                    groundCover.name = $"GroundCover_{x}_{z}_{attempt}";
                    count++;
                }
            }
        }

        return count;
    }
}