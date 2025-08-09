#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var gen = (DungeonGenerator)target;

        GUILayout.Space(8);
        if (GUILayout.Button("Generate Now"))
        {
            gen.Generate();
            EditorUtility.SetDirty(gen.gameObject);
        }
    }
}
#endif
