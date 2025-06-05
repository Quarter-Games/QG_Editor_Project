using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class SceneLoaderToolbarInjector
{
    public const string LastSceneKey = "QG_LastScenePath";
    static SceneLoaderToolbarInjector()
    {
        EditorApplication.update += InjectToolbar;

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            var lastScene = EditorPrefs.GetString(LastSceneKey, "");
            if (!string.IsNullOrEmpty(lastScene) && System.IO.File.Exists(lastScene))
            {

                // Load the last scene
                EditorSceneManager.OpenScene(lastScene, OpenSceneMode.Single);
                EditorPrefs.DeleteKey(LastSceneKey); // Clear the last scene key after loading
            }
            else
            {
                Debug.LogWarning("No valid last scene found to return to.");
            }
        }
    }

    private static bool isInjected = false;

    private static void InjectToolbar()
    {
        if (isInjected) return;

        // Find the toolbar
        var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
        if (toolbars == null || toolbars.Length == 0) return;

        var toolbar = toolbars[0];
        var rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        if (rootField == null) return;

        var root = rootField.GetValue(toolbar) as VisualElement;
        if (root == null) return;

        var container = new IMGUIContainer(() =>
        {
            DrawSceneLoaderToolbar();
        });

        // Optional: Wrap in a VisualElement with fixed width if needed
        var parent = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        parent.Add(container);

        // Inject into the toolbar root
        root.Query("ToolbarZoneLeftAlign").First()?.Add(parent);

        isInjected = true;
        EditorApplication.update -= InjectToolbar;
    }

    static void DrawSceneLoaderToolbar()
    {
        string[] sceneNames = EditorBuildSettings.scenes.Select(x => AssetDatabase.LoadAssetAtPath<SceneAsset>(x.path)?.name).Where(x => x != null).ToArray();

        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (sceneNames.Length == 0)
        {
            GUILayout.Label("No scenes found.");
            return;
        }

        if (!EditorPrefs.HasKey("SceneLoaderToolbar_SelectedScene"))
            EditorPrefs.SetInt("SceneLoaderToolbar_SelectedScene", 0);

        int selected = EditorPrefs.GetInt("SceneLoaderToolbar_SelectedScene");
        int newSelected = EditorGUILayout.Popup(selected, sceneNames, GUILayout.Width(150));

        if (newSelected != selected)
        {
            EditorPrefs.SetInt("SceneLoaderToolbar_SelectedScene", newSelected);
        }

        if (GUILayout.Button("Open", GUILayout.Width(60)))
        {
            OpenScene(newSelected);
        }

        if (GUILayout.Button("Play From", GUILayout.Width(90)))
        {
            PlayFromScene(newSelected);
        }
        GUILayout.EndHorizontal();
    }

    static void OpenScene(int index)
    {
        string scenePath = EditorBuildSettings.scenes.Where(x=> System.IO.File.Exists(x.path)).ToList()[index].path;
        if (!string.IsNullOrEmpty(scenePath) && System.IO.File.Exists(scenePath))
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(scenePath);
        }
        else
        {
            Debug.LogError("Scene path is invalid or does not exist: " + scenePath);
        }
    }

    [MenuItem("Tools/Start From Zero Scene %#I", false, 1000)] // Ctrl+Shift+L (Windows) or Cmd+Shift+L (Mac)
    public static void StartFromZeroScene()
    {
        var zeroScene = EditorBuildSettings.scenes[0];
        if (zeroScene != null && System.IO.File.Exists(zeroScene.path))
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(zeroScene.path);
            EditorApplication.isPlaying = true;
        }
        else
        {
            Debug.LogError("Zero scene not found or does not exist.");
        }
    }
    static void PlayFromScene(int index)
    {
        string scenePath = EditorBuildSettings.scenes.Where(x => System.IO.File.Exists(x.path)).ToList()[index].path;
        if (!string.IsNullOrEmpty(scenePath) && System.IO.File.Exists(scenePath))
        {
            var currentScenePath = EditorSceneManager.GetActiveScene().path;
            EditorPrefs.SetString(LastSceneKey, currentScenePath);
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(scenePath);
            EditorApplication.isPlaying = true;
        }
        else
        {
            Debug.LogError("Scene path is invalid or does not exist: " + scenePath);
        }
    }
}
