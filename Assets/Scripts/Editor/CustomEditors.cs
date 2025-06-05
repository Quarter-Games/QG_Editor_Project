using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MyComponent))]
public class MyComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("showAdvanced"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(MyScriptableData))]
public class MyScriptableDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableExtraSettings"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("powerLevel"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("previewImage"));

        serializedObject.ApplyModifiedProperties();
    }
}
