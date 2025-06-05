using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class AssetCleanupTool : EditorWindow
{
    
    static readonly string UnusedAssetsSearchFolder = "Assets/UnusedAssetsTest";
    static readonly string UnusedAssetsMoveFolder = "Assets/UnusedAssets";

    [MenuItem("Tools/Clean Up Unused Assets %#u")] // Ctrl+Shift+U
    public static void ShowWindow()
    {
        CleanUpUnusedAssets();
    }

    public static void CleanUpUnusedAssets()
    {
        
        if (!Directory.Exists(UnusedAssetsSearchFolder))
        {
            EditorUtility.DisplayDialog("Folder Not Found", $"The folder '{UnusedAssetsSearchFolder}' does not exist.\nCreate it and put some test assets there for this tool to work.", "OK");
            return;
        }

      
        string[] allFiles = Directory.GetFiles(UnusedAssetsSearchFolder, "*.*", SearchOption.AllDirectories);
        List<string> assetPaths = new List<string>();
        foreach (string file in allFiles)
        {
            if (file.EndsWith(".meta")) continue;
            string assetPath = file.Replace('\\', '/'); 
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
                assetPaths.Add(assetPath);
        }

        if (assetPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Unused Assets Cleanup", $"No assets found in '{UnusedAssetsSearchFolder}'!", "OK");
            return;
        }

      
        int result = EditorUtility.DisplayDialogComplex(
            "Unused Assets Cleanup",
            $"We found {assetPaths.Count} assets in '{UnusedAssetsSearchFolder}'. What would you like to do?",
            "Delete All",                     
            "Move to 'UnusedAssets' Folder",  
            "Cancel"                          
        );

        switch (result)
        {
            case 0: 
                if (EditorUtility.DisplayDialog("Are you sure?",
                    $"This will permanently delete {assetPaths.Count} assets!\n\nContinue?", "Yes", "No"))
                {
                    int deletedCount = 0;
                    foreach (string path in assetPaths)
                    {
                        if (AssetDatabase.DeleteAsset(path))
                        {
                            Debug.Log($"Deleted: {path}");
                            deletedCount++;
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to delete: {path}");
                        }
                    }
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("Done", $"Deleted {deletedCount} assets.", "OK");
                }
                else
                {
                    Debug.Log("Delete cancelled by user.");
                }
                break;

            case 1: 
                if (!AssetDatabase.IsValidFolder(UnusedAssetsMoveFolder))
                {
                    string guid = AssetDatabase.CreateFolder("Assets", "UnusedAssets");
                    if (string.IsNullOrEmpty(guid))
                    {
                        EditorUtility.DisplayDialog("Error", "Failed to create destination folder.", "OK");
                        return;
                    }
                }

                int movedCount = 0;
                foreach (string path in assetPaths)
                {
                    string fileName = Path.GetFileName(path);
                    string newPath = Path.Combine(UnusedAssetsMoveFolder, fileName).Replace("\\", "/");
                    string uniqueNewPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

                    var moveResult = AssetDatabase.MoveAsset(path, uniqueNewPath);
                    if (string.IsNullOrEmpty(moveResult))
                    {
                        Debug.Log($"Moved: {path} -> {uniqueNewPath}");
                        movedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to move {path}: {moveResult}");
                    }
                }
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Done", $"Moved {movedCount} assets to '{UnusedAssetsMoveFolder}'.", "OK");
                break;

            case 2: 
                Debug.Log("Cleanup cancelled.");
                break;
        }
    }
}
