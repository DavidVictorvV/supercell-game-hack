using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class SceneManagerWindow : EditorWindow
{
    // Spacer class for bottom padding
    private class Spacer { }

    // Constants
    private const string MENU_ITEM = "Tools/Scene Manager";
    private const int MIN_WIDTH = 800;
    private const int MIN_HEIGHT = 500;
    private const int CATEGORY_PANEL_WIDTH = 200;

    // UI Elements
    private UnityEditor.UIElements.ToolbarSearchField searchField;
    private UnityEditor.UIElements.ToolbarToggle packageCacheToggle;
    private ListView categoryList;
    private ListView sceneListView;
    private TwoPaneSplitView splitView;

    // Data
    private SceneManagerData data;
    private List<SceneInfo> allScenes;
    private List<SceneInfo> filteredScenes;
    private List<object> displayItems; // Mix of folder headers (strings) and scenes (SceneInfo)
    private string[] categoryNames = new string[]
    {
        "All",
        "Favorites",
        "Recent",
        "Custom",
        "External Assets",
        "GameCreator Examples",
        "Asset Inventory",
        "Other"
    };

    [MenuItem(MENU_ITEM, priority = 100)]
    public static void ShowWindow()
    {
        SceneManagerWindow window = GetWindow<SceneManagerWindow>();
        window.titleContent = new GUIContent("Scene Manager");
        window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
        window.Show();
    }

    private void OnEnable()
    {
        // Load data
        data = new SceneManagerData();
        data.Load();

        // Discover all scenes
        DiscoverScenes();

        // Build UI
        BuildUI();

        // Apply initial filter
        FilterScenes(string.Empty, data.lastSelectedCategory);
    }

    private void OnDisable()
    {
        // Save data when window closes
        if (data != null)
        {
            data.Save();
        }
    }

    private void DiscoverScenes()
    {
        allScenes = new List<SceneInfo>();

        // Find all scene assets
        string[] guids = AssetDatabase.FindAssets("t:Scene");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Filter out package cache by default
            if (!data.showPackageCacheScenes &&
                (path.StartsWith("Library/PackageCache") || path.StartsWith("Packages/")))
            {
                continue;
            }

            SceneInfo info = new SceneInfo(path);
            allScenes.Add(info);
        }

        // Sort alphabetically by name
        allScenes.Sort((a, b) => string.Compare(a.sceneName, b.sceneName, System.StringComparison.OrdinalIgnoreCase));
    }

    private void BuildUI()
    {
        // Clear root
        rootVisualElement.Clear();

        // Create toolbar
        UnityEditor.UIElements.Toolbar toolbar = new UnityEditor.UIElements.Toolbar();
        toolbar.style.flexShrink = 0;

        // Search field
        searchField = new UnityEditor.UIElements.ToolbarSearchField();
        searchField.style.flexGrow = 1;
        searchField.style.minWidth = 200;
        searchField.RegisterValueChangedCallback(evt =>
        {
            FilterScenes(evt.newValue, categoryList?.selectedIndex ?? 0);
        });
        toolbar.Add(searchField);

        // Package cache toggle
        packageCacheToggle = new UnityEditor.UIElements.ToolbarToggle();
        packageCacheToggle.text = "Show Package Cache";
        packageCacheToggle.value = data.showPackageCacheScenes;
        packageCacheToggle.RegisterValueChangedCallback(evt =>
        {
            data.showPackageCacheScenes = evt.newValue;
            data.Save();
            DiscoverScenes();
            FilterScenes(searchField.value, categoryList.selectedIndex);
        });
        toolbar.Add(packageCacheToggle);

        rootVisualElement.Add(toolbar);

        // Create split view
        splitView = new TwoPaneSplitView(0, CATEGORY_PANEL_WIDTH, TwoPaneSplitViewOrientation.Horizontal);
        splitView.style.flexGrow = 1;

        // Left panel - Category list
        categoryList = new ListView();
        categoryList.style.flexGrow = 1;
        categoryList.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        categoryList.selectionType = SelectionType.Single;
        categoryList.itemsSource = categoryNames.ToList();
        categoryList.makeItem = MakeCategoryItem;
        categoryList.bindItem = BindCategoryItem;
        categoryList.selectedIndex = data.lastSelectedCategory;
        categoryList.onSelectionChange += (items) =>
        {
            var selected = items.FirstOrDefault();
            if (selected != null)
            {
                int index = categoryNames.ToList().IndexOf((string)selected);
                if (index >= 0)
                {
                    data.lastSelectedCategory = index;
                    FilterScenes(searchField.value, index);
                }
            }
        };

        splitView.Add(categoryList);

        // Right panel - Scene list
        sceneListView = new ListView();
        sceneListView.style.flexGrow = 1;
        sceneListView.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        sceneListView.selectionType = SelectionType.Single;
        sceneListView.makeItem = MakeSceneItem;
        sceneListView.bindItem = BindSceneItem;
        sceneListView.itemsSource = displayItems;
        sceneListView.selectionChanged += OnSceneSelectionChanged;

        splitView.Add(sceneListView);

        rootVisualElement.Add(splitView);
    }

    private VisualElement MakeCategoryItem()
    {
        VisualElement container = new VisualElement();
        container.style.paddingTop = 2;
        container.style.paddingBottom = 2;

        Label label = new Label();
        label.style.paddingLeft = 10;
        label.style.paddingTop = 6;
        label.style.paddingBottom = 6;
        label.style.paddingRight = 5;
        label.style.fontSize = 12;
        label.style.unityFontStyleAndWeight = FontStyle.Normal;

        container.Add(label);
        return container;
    }

    private void BindCategoryItem(VisualElement element, int index)
    {
        Label label = element.Q<Label>();
        if (label != null && index < categoryNames.Length)
        {
            string categoryName = categoryNames[index];
            int count = GetCategoryCount(index);

            // Add icon based on category
            string icon = index switch
            {
                0 => "📂", // All
                1 => "⭐", // Favorites
                2 => "🕐", // Recent
                3 => "🎮", // Custom
                4 => "📦", // External
                5 => "🎨", // GameCreator
                6 => "📊", // Asset Inventory
                7 => "📄", // Other
                _ => "•"
            };

            if (count > 0)
            {
                label.text = $"{icon} {categoryName} ({count})";
            }
            else
            {
                label.text = $"{icon} {categoryName}";
            }

            // Highlight if it's favorites or recent
            if (index == 1 || index == 2)
            {
                label.style.color = new Color(0.9f, 0.9f, 1f);
            }
            else
            {
                label.style.color = new Color(0.85f, 0.85f, 0.85f);
            }
        }
    }

    private int GetCategoryCount(int categoryIndex)
    {
        switch (categoryIndex)
        {
            case 0: // All
                return allScenes.Count;
            case 1: // Favorites
                return data.GetFavoritesCount();
            case 2: // Recent
                return data.GetRecentCount();
            case 3: // Custom
                return allScenes.Count(s => s.category == SceneInfo.SceneCategory.Custom);
            case 4: // External Assets
                return allScenes.Count(s => s.category == SceneInfo.SceneCategory.ExternalAssets);
            case 5: // GameCreator Examples
                return allScenes.Count(s => s.category == SceneInfo.SceneCategory.GameCreatorExamples);
            case 6: // Asset Inventory
                return allScenes.Count(s => s.category == SceneInfo.SceneCategory.AssetInventory);
            case 7: // Other
                return allScenes.Count(s => s.category == SceneInfo.SceneCategory.Other);
            default:
                return 0;
        }
    }

    private VisualElement MakeSceneItem()
    {
        VisualElement container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;
        container.name = "scene-container";
        container.style.minHeight = 24;
        container.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        container.style.borderBottomWidth = 1;

        // Content label (for both folders and scenes)
        Label nameLabel = new Label();
        nameLabel.name = "scene-name";
        nameLabel.style.flexGrow = 1;
        nameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        nameLabel.style.fontSize = 12;
        nameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        container.Add(nameLabel);

        // Favorite button (only visible for scenes)
        Button favoriteButton = new Button();
        favoriteButton.name = "favorite-button";
        favoriteButton.text = "☆";
        favoriteButton.style.width = 30;
        favoriteButton.style.height = 24;
        favoriteButton.style.marginRight = 5;
        container.Add(favoriteButton);

        // Register event handlers once during creation
        // Store original background color for hover effect
        var originalBgColor = new StyleColor(StyleKeyword.Null);

        // Add hover effect
        container.RegisterCallback<MouseEnterEvent>(evt =>
        {
            var elem = evt.currentTarget as VisualElement;
            if (elem?.userData is SceneInfo)
            {
                originalBgColor = container.resolvedStyle.backgroundColor;
                container.style.backgroundColor = new Color(0.3f, 0.4f, 0.5f, 0.35f);
            }
        });

        container.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            var elem = evt.currentTarget as VisualElement;
            if (elem?.userData is SceneInfo && originalBgColor.value != Color.clear)
            {
                container.style.backgroundColor = originalBgColor;
            }
        });

        // Handle double-click to open scene
        container.RegisterCallback<MouseDownEvent>(evt =>
        {
            var elem = evt.currentTarget as VisualElement;
            if (elem?.userData is SceneInfo scene)
            {
                if (evt.clickCount == 2 && evt.button == 0)
                {
                    OnSceneDoubleClick(scene);
                    evt.StopPropagation();
                }
            }
        });

        // Handle right-click for context menu
        container.RegisterCallback<MouseDownEvent>(evt =>
        {
            var elem = evt.currentTarget as VisualElement;
            if (elem?.userData is SceneInfo scene)
            {
                if (evt.button == 1) // Right mouse button
                {
                    ShowContextMenu(scene);
                    evt.StopPropagation();
                }
            }
            else if (elem?.userData is string folderPath)
            {
                // Handle folder header click to toggle collapse
                if (evt.button == 0) // Left mouse button
                {
                    OnFolderHeaderClick(folderPath);
                    evt.StopPropagation();
                }
            }
        });

        return container;
    }

    private void BindSceneItem(VisualElement element, int index)
    {
        if (displayItems == null || index < 0 || index >= displayItems.Count)
            return;

        object item = displayItems[index];
        Label nameLabel = element.Q<Label>("scene-name");
        Button favButton = element.Q<Button>("favorite-button");
        VisualElement container = element.Q("scene-container");

        // Store the current item in userData to avoid duplicate event handlers
        element.userData = item;

        // Check if this is a spacer
        if (item is Spacer)
        {
            // Make invisible but take up space
            if (nameLabel != null)
            {
                nameLabel.text = "";
                nameLabel.style.display = DisplayStyle.None;
            }
            if (favButton != null)
            {
                favButton.style.display = DisplayStyle.None;
            }
            if (container != null)
            {
                container.style.backgroundColor = new Color(0, 0, 0, 0);
                container.style.minHeight = 30;
                container.style.borderBottomWidth = 0;
            }
            return;
        }

        // Check if this is a folder header
        if (item is string folderPath)
        {
            // This is a folder header
            if (nameLabel != null)
            {
                bool isCollapsed = data.IsFolderCollapsed(folderPath);
                string arrow = isCollapsed ? "▶" : "▼";

                // Get base path to strip based on current category
                string basePathToStrip = GetBasePathForCategory(categoryList?.selectedIndex ?? 0);
                string displayPath = StripBasePath(folderPath, basePathToStrip);

                nameLabel.text = $"{arrow} 📁 {displayPath}";
                nameLabel.style.paddingLeft = 8;
                nameLabel.style.paddingTop = 8;
                nameLabel.style.paddingBottom = 8;
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameLabel.style.fontSize = 11;
                nameLabel.style.color = new Color(0.85f, 0.85f, 1f, 1f);
            }

            if (favButton != null)
            {
                favButton.style.display = DisplayStyle.None;
            }

            if (container != null)
            {
                container.style.backgroundColor = new Color(0.25f, 0.3f, 0.4f, 0.6f);
                container.style.paddingLeft = 5;
                container.style.paddingRight = 5;
                container.style.marginTop = 2;
                container.style.marginBottom = 1;
                container.style.borderLeftWidth = 3;
                container.style.borderLeftColor = new Color(0.3f, 0.5f, 0.8f, 0.8f);
                // Cursor styling - folder headers are clickable (cursor change not critical)
            }
        }
        else if (item is SceneInfo scene)
        {
            // This is a scene
            if (nameLabel != null)
            {
                nameLabel.text = $"  {scene.sceneName}";
                nameLabel.style.paddingLeft = 30; // Indent under folder
                nameLabel.style.paddingTop = 4;
                nameLabel.style.paddingBottom = 4;
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                nameLabel.style.color = new Color(0.95f, 0.95f, 0.95f, 1f);
                nameLabel.style.fontSize = 12;
            }

            if (favButton != null)
            {
                favButton.style.display = DisplayStyle.Flex;
                favButton.text = data.IsFavorite(scene.scenePath) ? "★" : "☆";

                // Update button styling
                favButton.style.backgroundColor = new Color(0, 0, 0, 0);
                favButton.style.borderTopWidth = 0;
                favButton.style.borderBottomWidth = 0;
                favButton.style.borderLeftWidth = 0;
                favButton.style.borderRightWidth = 0;
                favButton.style.color = data.IsFavorite(scene.scenePath) ? new Color(1f, 0.84f, 0f) : new Color(0.5f, 0.5f, 0.5f);
                favButton.style.fontSize = 16;

                // Store the scene reference and add callback
                var sceneRef = scene;
                favButton.clickable = new Clickable(() => OnFavoriteToggle(sceneRef));
            }

            if (container != null)
            {
                // Count only scenes before this one for alternating colors
                int sceneIndex = 0;
                for (int i = 0; i < index; i++)
                {
                    if (displayItems[i] is SceneInfo)
                        sceneIndex++;
                }

                // Alternating background colors for better readability
                bool isEven = sceneIndex % 2 == 0;
                container.style.backgroundColor = isEven
                    ? new Color(0, 0, 0, 0)
                    : new Color(0.15f, 0.15f, 0.15f, 0.3f);

                container.style.paddingLeft = 0;
                container.style.paddingRight = 10;
            }
        }
    }

    private void OnSceneSelectionChanged(IEnumerable<object> selectedItems)
    {
        // Prevent folder headers and spacers from being selectable
        var selected = selectedItems.FirstOrDefault();
        if (selected is string || selected is Spacer)
        {
            sceneListView.ClearSelection();
        }
    }

    private void FilterScenes(string searchQuery, int categoryIndex)
    {
        filteredScenes = new List<SceneInfo>(allScenes);

        // Determine the base path to strip based on category
        string basePathToStrip = GetBasePathForCategory(categoryIndex);

        // Apply category filter
        switch (categoryIndex)
        {
            case 1: // Favorites
                filteredScenes = filteredScenes.Where(s => data.IsFavorite(s.scenePath)).ToList();
                break;
            case 2: // Recent
                filteredScenes = data.recentScenes.Take(10).ToList();
                break;
            case 3: // Custom
                filteredScenes = filteredScenes.Where(s => s.category == SceneInfo.SceneCategory.Custom).ToList();
                break;
            case 4: // External Assets
                filteredScenes = filteredScenes.Where(s => s.category == SceneInfo.SceneCategory.ExternalAssets).ToList();
                break;
            case 5: // GameCreator Examples
                filteredScenes = filteredScenes.Where(s => s.category == SceneInfo.SceneCategory.GameCreatorExamples).ToList();
                break;
            case 6: // Asset Inventory
                filteredScenes = filteredScenes.Where(s => s.category == SceneInfo.SceneCategory.AssetInventory).ToList();
                break;
            case 7: // Other
                filteredScenes = filteredScenes.Where(s => s.category == SceneInfo.SceneCategory.Other).ToList();
                break;
            // case 0: All - no filtering needed
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(searchQuery))
        {
            filteredScenes = filteredScenes.Where(s =>
                s.sceneName.IndexOf(searchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();
        }

        // Group by folder and create display list with headers
        displayItems = new List<object>();

        var groupedByFolder = filteredScenes
            .GroupBy(s => s.folderPath)
            .OrderBy(g => g.Key);

        foreach (var group in groupedByFolder)
        {
            string folderPath = group.Key ?? "Unknown";

            // Strip the base path for display
            string displayPath = StripBasePath(folderPath, basePathToStrip);

            // Add folder header with both full path (for identification) and display path
            // Store as a tuple or custom object to keep both
            displayItems.Add(folderPath); // Still use full path for collapse tracking

            // Add scenes in this folder (sorted alphabetically) only if not collapsed
            if (!data.IsFolderCollapsed(folderPath))
            {
                foreach (var scene in group.OrderBy(s => s.sceneName))
                {
                    displayItems.Add(scene);
                }
            }
        }

        // Add spacer elements at the end to prevent last scene from being cut off
        displayItems.Add(new Spacer());
        displayItems.Add(new Spacer());

        // Update list view
        if (sceneListView != null)
        {
            sceneListView.itemsSource = displayItems;
            sceneListView.Rebuild();
        }

        // Update category counts
        if (categoryList != null)
        {
            categoryList.Rebuild();
        }
    }

    private void OnSceneDoubleClick(SceneInfo scene)
    {
        if (scene == null || string.IsNullOrEmpty(scene.scenePath))
            return;

        // Check for unsaved changes
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            // Open the scene
            EditorSceneManager.OpenScene(scene.scenePath);

            // Track in recents
            scene.lastAccessTime = System.DateTime.Now;
            data.AddRecentScene(scene);
            data.Save();

            // Refresh UI if on "Recent" category
            if (categoryList.selectedIndex == 2)
            {
                FilterScenes(searchField.value, 2);
            }
        }
    }

    private void OnFavoriteToggle(SceneInfo scene)
    {
        if (scene == null || string.IsNullOrEmpty(scene.scenePath))
            return;

        if (data.IsFavorite(scene.scenePath))
        {
            data.RemoveFavorite(scene.scenePath);
        }
        else
        {
            data.AddFavorite(scene.scenePath);
        }

        data.Save();

        // Refresh the list
        FilterScenes(searchField.value, categoryList.selectedIndex);
    }

    private void ShowContextMenu(SceneInfo scene)
    {
        if (scene == null)
            return;

        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("Open Scene"), false, () => OnSceneDoubleClick(scene));
        menu.AddItem(new GUIContent("Add to Build Settings"), false, () => AddToBuildSettings(scene));
        menu.AddSeparator("");

        string favText = data.IsFavorite(scene.scenePath) ? "Remove from Favorites" : "Add to Favorites";
        menu.AddItem(new GUIContent(favText), false, () => OnFavoriteToggle(scene));

        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Show in Project"), false, () =>
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.scenePath);
            if (sceneAsset != null)
            {
                EditorGUIUtility.PingObject(sceneAsset);
                Selection.activeObject = sceneAsset;
            }
        });

        menu.ShowAsContext();
    }

    private void AddToBuildSettings(SceneInfo scene)
    {
        if (scene == null || string.IsNullOrEmpty(scene.scenePath))
            return;

        var buildScenes = EditorBuildSettings.scenes.ToList();

        // Check if already in build settings
        if (buildScenes.Any(s => s.path == scene.scenePath))
        {
            Debug.Log($"Scene '{scene.sceneName}' is already in Build Settings");
            return;
        }

        buildScenes.Add(new EditorBuildSettingsScene(scene.scenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();

        Debug.Log($"Added '{scene.sceneName}' to Build Settings");
    }

    private void OnFolderHeaderClick(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
            return;

        // Toggle collapsed state
        data.ToggleFolderCollapsed(folderPath);
        data.Save();

        // Refresh the display
        FilterScenes(searchField.value, categoryList.selectedIndex);
    }

    private string GetBasePathForCategory(int categoryIndex)
    {
        switch (categoryIndex)
        {
            case 3: // Custom
                return "Assets/Scenes/";
            case 4: // External Assets
                return "Assets/External/";
            case 5: // GameCreator Examples
                return "Assets/Plugins/GameCreator/Installs/";
            case 6: // Asset Inventory
                return "Assets/AssetInventory/";
            default:
                return ""; // No base path stripping for All, Favorites, Recent, Other
        }
    }

    private string StripBasePath(string fullPath, string basePath)
    {
        if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(fullPath))
            return fullPath;

        // Normalize separators
        string normalizedFull = fullPath.Replace("\\", "/");
        string normalizedBase = basePath.Replace("\\", "/");

        if (normalizedFull.StartsWith(normalizedBase, System.StringComparison.OrdinalIgnoreCase))
        {
            string stripped = normalizedFull.Substring(normalizedBase.Length);
            return string.IsNullOrEmpty(stripped) ? fullPath : stripped;
        }

        return fullPath;
    }
}
