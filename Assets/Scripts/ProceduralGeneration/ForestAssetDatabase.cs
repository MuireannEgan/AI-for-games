using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum ForestSeason
{
    Normal = 1,
    Autumn = 2,
    Winter = 4
}

public enum ForestAssetType
{
    Tree,
    Bush,
    Rock,
    GroundCover,
    Prop
}

[Serializable]
public class ForestAssetEntry
{
    public GameObject prefab;
    public ForestAssetType type;
    public ForestSeason seasons;

    [Min(0f)]
    public float spawnWeight = 1f;
}

[CreateAssetMenu(
    fileName = "ForestAssetDatabase",
    menuName = "Forest/Asset Database"
)]
public class ForestAssetDatabase : ScriptableObject
{
    public List<ForestAssetEntry> assets =
        new List<ForestAssetEntry>();

    public int GetEligibleCount(
        ForestAssetType type,
        ForestSeason seasons)
    {
        int count = 0;

        foreach (ForestAssetEntry entry in assets)
        {
            if (Eligible(entry, type, seasons))
                count++;
        }

        return count;
    }

    public GameObject GetRandom(
        ForestAssetType type,
        ForestSeason season,
        System.Random rng)
    {
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        double total = 0;

        foreach (ForestAssetEntry entry in assets)
        {
            if (Eligible(entry, type, season))
                total += entry.spawnWeight;
        }

        if (total <= 0)
            return null;

        double selection = rng.NextDouble() * total;
        GameObject fallback = null;

        foreach (ForestAssetEntry entry in assets)
        {
            if (!Eligible(entry, type, season))
                continue;

            fallback = entry.prefab;
            selection -= entry.spawnWeight;

            if (selection < 0)
                return entry.prefab;
        }

        return fallback;
    }

    private static bool Eligible(
        ForestAssetEntry entry,
        ForestAssetType type,
        ForestSeason seasons)
    {
        return entry != null
            && entry.prefab != null
            && entry.type == type
            && entry.spawnWeight > 0
            && (entry.seasons & seasons) != 0;
    }
}