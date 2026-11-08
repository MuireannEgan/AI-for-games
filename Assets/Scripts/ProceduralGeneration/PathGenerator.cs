using System.Collections.Generic;
using UnityEngine;

// Generates winding paths, path meshes and tiles reserved for navigation.
public static class PathGenerator
{
    public static void GeneratePaths(ForestGenerationContext c, System.Random random, Vector2Int altarPosition)
    {
        foreach (Vector2Int cave in c.cavePositions)
        {
            List<Vector3> path = GenerateWindingPath(c, altarPosition, cave, random);

            c.generatedPaths.Add(path);
            ReservePathTiles(c, path);
            CreatePathMesh(c, path, random);
        }
    }

    private static List<Vector3> GenerateWindingPath(ForestGenerationContext c,
        Vector2Int start,
        Vector2Int end,
        System.Random random)
    {
        Vector3 startPoint = new Vector3(start.x * c.tileSize, 0f, start.y * c.tileSize);
        Vector3 endPoint = new Vector3(end.x * c.tileSize, 0f, end.y * c.tileSize);

        Vector3 direction = endPoint - startPoint;
        float totalDistance = direction.magnitude;

        Vector3 perpendicular = new Vector3(-direction.z, 0f, direction.x).normalized;

        List<Vector3> waypoints = new List<Vector3> { startPoint };

        for (int i = 1; i <= c.intermediateWaypoints; i++)
        {
            float t = (float)i / (c.intermediateWaypoints + 1);
            Vector3 basePoint = Vector3.Lerp(startPoint, endPoint, t);

            float randomOffset = (float)random.NextDouble() * 2f - 1f;

            float maximumBend = Mathf.Min(
                totalDistance * c.pathCurvature,
                totalDistance / (c.intermediateWaypoints + 1) * 0.65f
            );

            float envelope = Mathf.Sin(t * Mathf.PI);

            Vector3 waypoint = basePoint +
                perpendicular * randomOffset * maximumBend * envelope;

            waypoints.Add(ClampToMap(c, waypoint));
        }

        waypoints.Add(endPoint);

        // Catmull-Rom interpolation creates a smooth curve through the waypoints.
        List<Vector3> smoothPath = new List<Vector3>();

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 p0 = waypoints[Mathf.Max(i - 1, 0)];
            Vector3 p1 = waypoints[i];
            Vector3 p2 = waypoints[i + 1];
            Vector3 p3 = waypoints[Mathf.Min(i + 2, waypoints.Count - 1)];

            for (int j = 0; j < c.samplesPerSection; j++)
            {
                float t = (float)j / c.samplesPerSection;
                Vector3 point = CatmullRom(c, p0, p1, p2, p3, t);

                smoothPath.Add(ClampToMap(c, point));
            }
        }

        smoothPath.Add(endPoint);
        return smoothPath;
    }

    private static Vector3 CatmullRom(ForestGenerationContext c,
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

    private static Vector3 ClampToMap(ForestGenerationContext c, Vector3 point)
    {
        float margin = c.pathWidth * (1f + c.pathWidthVariation) * 0.5f;

        return new Vector3(
            Mathf.Clamp(point.x, margin, (c.width - 1) * c.tileSize - margin),
            0f,
            Mathf.Clamp(point.z, margin, (c.height - 1) * c.tileSize - margin)
        );
    }

    // Reserve tiles near each path segment to keep trees away from the route.
    private static void ReservePathTiles(ForestGenerationContext c, List<Vector3> path)
    {
        float maximumHalfWidth = c.pathWidth * (1f + c.pathWidthVariation) * 0.5f;

        float clearance = maximumHalfWidth +
            c.treePathClearance +
            c.treePositionVariation * 1.4143f;

        for (int x = 0; x < c.width; x++)
        {
            for (int z = 0; z < c.height; z++)
            {
                Vector2Int tile = new Vector2Int(x, z);
                Vector3 centre = new Vector3(x * c.tileSize, 0f, z * c.tileSize);

                for (int i = 0; i < path.Count - 1; i++)
                {
                    float distance = DistanceToSegment(centre, path[i], path[i + 1]);

                    if (distance <= clearance)
                    {
                        c.walkablePaths.Add(tile);
                        break;
                    }
                }
            }
        }
    }

    public static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
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

    private static void CreatePathMesh(ForestGenerationContext c, List<Vector3> path, System.Random random)
    {
        GameObject pathObject = new GameObject("WindingDirtPath");
        pathObject.transform.SetParent(c.generatedParent, false);

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
            float currentWidth = c.pathWidth * (1f + widthNoise * c.pathWidthVariation);

            Vector3 centre = path[i] + Vector3.up * 0.06f;

            vertices[i * 2] = centre - side * currentWidth * 0.5f;
            vertices[i * 2 + 1] = centre + side * currentWidth * 0.5f;

            if (i > 0)
                accumulatedDistance += Vector3.Distance(path[i - 1], path[i]);

            uvs[i * 2] = new Vector2(0f, accumulatedDistance / c.pathWidth);
            uvs[i * 2 + 1] = new Vector2(1f, accumulatedDistance / c.pathWidth);
        }

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
        meshRenderer.sharedMaterial = c.dirtPathMaterial;
    }

}