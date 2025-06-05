using UnityEngine;
using UnityEditor;

public static class SceneViewTools
{
    [MenuItem("Tools/Focus Scene View on Player Start %#o")]
    private static void FocusSceneView()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("No GameObject with tag 'Player' found.");
            return;
        }

        SceneView.lastActiveSceneView.LookAt(player.transform.position);
    }
}
