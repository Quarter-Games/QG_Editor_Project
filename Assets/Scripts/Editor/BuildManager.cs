using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PreBuildManager : IPreprocessBuildWithReport
{
    List<string> titles = new List<string>
    {
        "Collecting collections...",
        "Building builds...",
        "Calculating calculations...",
        "Creating creations...",
        "Processing processes...",
        "Compiling compilations...",
        "Generating generations...",
        "Configuring configurations...",
        "Initializing initializations...",
        "Finalizing finalizations...",
        "Organizing organizations...",
        "Structuring structures...",
        "Formulating formulations...",
        "Arranging arrangements...",
        "Designing designs...",
        "Developing developments...",
        "Managing managements...",
        "Optimizing optimizations...",
        "Executing executions...",
        "Reviewing reviews..."
    };
    public int callbackOrder => -1;


    public void OnPreprocessBuild(BuildReport report)
    {
        int titlesCount = titles.Count;
        for (int i = 0; i < 100_000_000; i++)
        {
            float progress = i / 100_000_000f;
            int titleIndex = (i / 5_000_000) % titlesCount;
            string title = titles[titleIndex];
            string info = titles[titleIndex];
            EditorUtility.DisplayProgressBar(title, info, progress);
        }
        EditorUtility.ClearProgressBar();

    }
}
public class AfterBuildProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => int.MaxValue;
    public void OnPostprocessBuild(BuildReport report)
    {
        // Attempt to close Unity Editor after build
        Application.OpenURL("https://youtu.be/dQw4w9WgXcQ?si=fYJCd2fu9hz5Gbai");
        EditorApplication.Exit(0);
    }

}

[InitializeOnLoad]
public class BuildPipeLineSceneRemover
{
    static BuildPipeLineSceneRemover()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuild);
    }

    private static void OnBuild(BuildPlayerOptions options)
    {
        int decision = -1;
        if (decision < 0)
        {
            decision = EditorUtility.DisplayDialogComplex("Russian Roulette", "Do you want to remove random scene from the build?", "Yes", "Cancel", "No");
        }
        if (decision == 0)
        {
            var scenes = options.scenes.ToList();
            Debug.Log($"Initial scenes count: {scenes.Count}");
            if (scenes.Count > 1)
            {
                int randomIndex = UnityEngine.Random.Range(0, scenes.Count);
                scenes.RemoveAt(randomIndex);
                options.scenes = scenes.ToArray();
                Debug.Log($"Removed scene at index {randomIndex}. Remaining scenes: {scenes.Count}");
            }
            else
            {
                Debug.LogWarning("Cannot remove scene: only one scene is available in the build.");
            }
            BuildPipeline.BuildPlayer(options);
        }
        else if (decision == 1)
        {
            Debug.Log("Build process cancelled by the user.");
            return; // Cancel the build process
        }
        else
        {
            Debug.Log("Build process continued without removing any scenes.");
            BuildPipeline.BuildPlayer(options);
        }
    }
}