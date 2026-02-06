using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
public class SceneManagerData
{
    // EditorPrefs keys
    private const string PREFS_FAVORITES = "SceneManager:Favorites";
    private const string PREFS_RECENT_SCENES = "SceneManager:RecentScenes";
    private const string PREFS_COLLAPSED_FOLDERS = "SceneManager:CollapsedFolders";
    private const string PREFS_LAST_CATEGORY = "SceneManager:LastCategory";
    private const string PREFS_SHOW_PACKAGE_CACHE = "SceneManager:ShowPackageCache";
    private const int MAX_RECENT_SCENES = 10;

    // Serializable wrapper for HashSet
    [Serializable]
    private class FavoritesData
    {
        public List<string> paths = new List<string>();
    }

    [Serializable]
    private class RecentScenesData
    {
        public List<SceneInfo> scenes = new List<SceneInfo>();
    }

    // Data members
    public HashSet<string> favorites = new HashSet<string>();
    public List<SceneInfo> recentScenes = new List<SceneInfo>();
    public HashSet<string> collapsedFolders = new HashSet<string>();
    public int lastSelectedCategory = 0;
    public bool showPackageCacheScenes = false;

    public void Load()
    {
        // Load favorites
        if (EditorPrefs.HasKey(PREFS_FAVORITES))
        {
            string json = EditorPrefs.GetString(PREFS_FAVORITES);
            try
            {
                FavoritesData favData = JsonUtility.FromJson<FavoritesData>(json);
                if (favData != null && favData.paths != null)
                {
                    favorites = new HashSet<string>(favData.paths);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load favorites: {e.Message}");
                favorites = new HashSet<string>();
            }
        }

        // Load recent scenes
        if (EditorPrefs.HasKey(PREFS_RECENT_SCENES))
        {
            string json = EditorPrefs.GetString(PREFS_RECENT_SCENES);
            try
            {
                RecentScenesData recentData = JsonUtility.FromJson<RecentScenesData>(json);
                if (recentData != null && recentData.scenes != null)
                {
                    // Filter out deleted scenes
                    recentScenes = recentData.scenes
                        .Where(s => !string.IsNullOrEmpty(s.scenePath) && System.IO.File.Exists(s.scenePath))
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load recent scenes: {e.Message}");
                recentScenes = new List<SceneInfo>();
            }
        }

        // Load collapsed folders
        if (EditorPrefs.HasKey(PREFS_COLLAPSED_FOLDERS))
        {
            string json = EditorPrefs.GetString(PREFS_COLLAPSED_FOLDERS);
            try
            {
                FavoritesData collapsedData = JsonUtility.FromJson<FavoritesData>(json);
                if (collapsedData != null && collapsedData.paths != null)
                {
                    collapsedFolders = new HashSet<string>(collapsedData.paths);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load collapsed folders: {e.Message}");
                collapsedFolders = new HashSet<string>();
            }
        }

        // Load UI state
        lastSelectedCategory = EditorPrefs.GetInt(PREFS_LAST_CATEGORY, 0);
        showPackageCacheScenes = EditorPrefs.GetBool(PREFS_SHOW_PACKAGE_CACHE, false);
    }

    public void Save()
    {
        // Save favorites
        FavoritesData favData = new FavoritesData { paths = favorites.ToList() };
        string favJson = JsonUtility.ToJson(favData);
        EditorPrefs.SetString(PREFS_FAVORITES, favJson);

        // Save recent scenes
        RecentScenesData recentData = new RecentScenesData { scenes = recentScenes };
        string recentJson = JsonUtility.ToJson(recentData);
        EditorPrefs.SetString(PREFS_RECENT_SCENES, recentJson);

        // Save collapsed folders
        FavoritesData collapsedData = new FavoritesData { paths = collapsedFolders.ToList() };
        string collapsedJson = JsonUtility.ToJson(collapsedData);
        EditorPrefs.SetString(PREFS_COLLAPSED_FOLDERS, collapsedJson);

        // Save UI state
        EditorPrefs.SetInt(PREFS_LAST_CATEGORY, lastSelectedCategory);
        EditorPrefs.SetBool(PREFS_SHOW_PACKAGE_CACHE, showPackageCacheScenes);
    }

    public void AddFavorite(string scenePath)
    {
        if (!string.IsNullOrEmpty(scenePath))
        {
            favorites.Add(scenePath);
        }
    }

    public void RemoveFavorite(string scenePath)
    {
        if (!string.IsNullOrEmpty(scenePath))
        {
            favorites.Remove(scenePath);
        }
    }

    public bool IsFavorite(string scenePath)
    {
        return !string.IsNullOrEmpty(scenePath) && favorites.Contains(scenePath);
    }

    public void AddRecentScene(SceneInfo scene)
    {
        if (scene == null || string.IsNullOrEmpty(scene.scenePath))
            return;

        // Remove existing entry if present
        recentScenes.RemoveAll(s => s.scenePath == scene.scenePath);

        // Add to front of list
        scene.lastAccessTime = DateTime.Now;
        recentScenes.Insert(0, scene);

        // Trim to max size
        if (recentScenes.Count > MAX_RECENT_SCENES)
        {
            recentScenes = recentScenes.Take(MAX_RECENT_SCENES).ToList();
        }
    }

    public int GetFavoritesCount()
    {
        return favorites.Count;
    }

    public int GetRecentCount()
    {
        return Math.Min(recentScenes.Count, MAX_RECENT_SCENES);
    }

    public void ToggleFolderCollapsed(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
            return;

        if (collapsedFolders.Contains(folderPath))
        {
            collapsedFolders.Remove(folderPath);
        }
        else
        {
            collapsedFolders.Add(folderPath);
        }
    }

    public bool IsFolderCollapsed(string folderPath)
    {
        return !string.IsNullOrEmpty(folderPath) && collapsedFolders.Contains(folderPath);
    }
}
