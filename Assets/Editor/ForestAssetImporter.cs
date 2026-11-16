#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ForestAssetImporter
{
    private const string Root =
        "Assets/Environment/Nature";

    private const string ManifestPath =
        Root + "/forest_asset_manifest.csv";

    private const string DatabasePath =
        Root + "/ForestAssetDatabase.asset";

    [MenuItem("Tools/Forest/Build Asset Database")]
    public static void BuildDatabase()
    {
        TextAsset manifest =
            AssetDatabase.LoadAssetAtPath<TextAsset>(
                ManifestPath);

        if (manifest == null)
        {
            Debug.LogError(
                "Cannot find manifest: " + ManifestPath);
            return;
        }

        var entries = new List<ForestAssetEntry>();
        int missing = 0;

        string[] lines = manifest.text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line))
                continue;

            string[] columns = line.Split(',');

            // CSV columns:
            // relative_path,filename,asset_type,compatible_seasons
            if (columns.Length != 4)
            {
                Debug.LogWarning(
                    "Unexpected CSV row: " + line);
                continue;
            }

            string relativePath =
                columns[0].Trim().Replace('\\', '/');

            if (!relativePath.StartsWith(
                    "Forest/", StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    "Invalid relative path: " + relativePath);
                continue;
            }

            string modelPath = Root + "/" +
                relativePath.Substring("Forest/".Length);

            GameObject model =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    modelPath);

            if (model == null)
            {
                Debug.LogWarning(
                    "Missing FBX: " + modelPath);

                missing++;
                continue;
            }

            if (!Enum.TryParse(
                    columns[2].Trim(),
                    true,
                    out ForestAssetType type))
            {
                Debug.LogWarning(
                    "Invalid type: " + line);
                continue;
            }

            ForestSeason seasons = 0;

            foreach (string token in
                     columns[3].Trim().Split('|'))
            {
                if (Enum.TryParse(
                        token.Trim(),
                        true,
                        out ForestSeason season))
                {
                    seasons |= season;
                }
            }

            if (seasons == 0)
                continue;

            entries.Add(new ForestAssetEntry
            {
                prefab = model,
                type = type,
                seasons = seasons,
                spawnWeight = 1f
            });
        }

        if (entries.Count == 0 || missing > 0)
        {
            Debug.LogError(
                $"Database NOT updated: {entries.Count} valid, " +
                $"{missing} missing. Check the import path.");
            return;
        }

        ForestAssetDatabase database =
            AssetDatabase.LoadAssetAtPath<ForestAssetDatabase>(
                DatabasePath);

        if (database == null)
        {
            database =
                ScriptableObject.CreateInstance<
                    ForestAssetDatabase>();

            AssetDatabase.CreateAsset(
                database, DatabasePath);
        }

        Undo.RecordObject(
            database, "Build forest asset database");

        database.assets = entries;

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Selection.activeObject = database;

        Debug.Log(
            $"Forest database built: " +
            $"{entries.Count} models, {missing} missing.");
    }
}
#endif