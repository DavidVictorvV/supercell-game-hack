using System;
using UnityEngine;

[Serializable]
public class SceneInfo
{
    public enum SceneCategory
    {
        Custom,              // Assets/Scenes/
        ExternalAssets,      // Assets/External/
        GameCreatorExamples, // Assets/Plugins/GameCreator/Installs/
        AssetInventory,      // Assets/AssetInventory/
        PackageCache,        // Library/PackageCache
        Other                // Everything else
    }

    public string scenePath;
    public string sceneName;
    public SceneCategory category;
    public string folderPath;
    public DateTime lastAccessTime;

    public SceneInfo()
    {
        // Parameterless constructor for JsonUtility
    }

    public SceneInfo(string path)
    {
        scenePath = path;
        sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
        category = DetermineCategory(path);
        folderPath = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        lastAccessTime = DateTime.MinValue;
    }

    private SceneCategory DetermineCategory(string path)
    {
        // Normalize path separators
        string normalizedPath = path.Replace("\\", "/");

        if (normalizedPath.StartsWith("Assets/Scenes/", StringComparison.OrdinalIgnoreCase))
        {
            return SceneCategory.Custom;
        }
        else if (normalizedPath.StartsWith("Assets/External/", StringComparison.OrdinalIgnoreCase))
        {
            return SceneCategory.ExternalAssets;
        }
        else if (normalizedPath.Contains("GameCreator") && normalizedPath.Contains("Installs"))
        {
            return SceneCategory.GameCreatorExamples;
        }
        else if (normalizedPath.StartsWith("Assets/AssetInventory/", StringComparison.OrdinalIgnoreCase))
        {
            return SceneCategory.AssetInventory;
        }
        else if (normalizedPath.StartsWith("Library/PackageCache", StringComparison.OrdinalIgnoreCase) ||
                 normalizedPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
        {
            return SceneCategory.PackageCache;
        }
        else
        {
            return SceneCategory.Other;
        }
    }

    public string GetCategoryDisplayName()
    {
        switch (category)
        {
            case SceneCategory.Custom:
                return "Custom";
            case SceneCategory.ExternalAssets:
                return "External Assets";
            case SceneCategory.GameCreatorExamples:
                return "GameCreator Examples";
            case SceneCategory.AssetInventory:
                return "Asset Inventory";
            case SceneCategory.PackageCache:
                return "Package Cache";
            case SceneCategory.Other:
                return "Other";
            default:
                return "Unknown";
        }
    }
}
