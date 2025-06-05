using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerController))]
public class PlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PlayerController player = (PlayerController)target; 
        player.labelColor = EditorGUILayout.ColorField("Label Color", player.labelColor);
        GUIStyle coloredLabel = new GUIStyle(EditorStyles.whiteBoldLabel);
        coloredLabel.normal.textColor = player.labelColor;
        GUILayout.Label("This is the player controller script", coloredLabel);
        base.OnInspectorGUI();
        GUILayout.Label("test", EditorStyles.foldout);
    }

}
