using System.Collections.Generic;
using UnityEngine;

/* Tracks occupied vegetation footprints to prevent overlapping placements. */
public class VegetationSpacing
{
    private readonly List<Bounds> occupiedBounds = new List<Bounds>();

    public bool TryRegister(
        GameObject instance,
        float spacingBuffer = 0f,
        float footprintScale = 1f)
    {
        if (instance == null)
            return false;

        if (!TryGetBounds(instance, out Bounds bounds))
            return true;

        float scale = Mathf.Clamp(footprintScale, 0.1f, 1f);
        float padding = Mathf.Max(0f, spacingBuffer);

        bounds.size = new Vector3(
            bounds.size.x * scale + padding * 2f,
            bounds.size.y,
            bounds.size.z * scale + padding * 2f);

        foreach (Bounds occupied in occupiedBounds)
        {
            if (OverlapsXZ(bounds, occupied))
                return false;
        }

        occupiedBounds.Add(bounds);
        return true;
    }

    private static bool OverlapsXZ(Bounds a, Bounds b)
    {
        return a.min.x < b.max.x &&
               a.max.x > b.min.x &&
               a.min.z < b.max.z &&
               a.max.z > b.min.z;
    }

    private static bool TryGetBounds(GameObject instance, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
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

    public void Clear()
    {
        occupiedBounds.Clear();
    }
}