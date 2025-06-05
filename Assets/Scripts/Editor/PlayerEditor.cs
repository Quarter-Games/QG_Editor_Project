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
        GUILayout.BeginHorizontal();
        GUILayout.Label("Move Speed");
        Color speedColor = player.moveSpeed < 0 ? Color.red : Color.green;
        GUIStyle style = new GUIStyle(GUI.skin.textField);
        style.normal.textColor = speedColor;
        string input = GUILayout.TextField(player.moveSpeed.ToString(), style, GUILayout.Width(100));
        if (float.TryParse(input, out float newSpeed))
        {
            if (newSpeed != player.moveSpeed)
            {
                Undo.RecordObject(player, "Change Move Speed");
                player.moveSpeed = newSpeed;
                EditorUtility.SetDirty(player);
            }
        }
        GUILayout.EndHorizontal();
        switch (player.moveSpeed)
        {
            case < 0:
                EditorGUILayout.HelpBox("Move speed cannot be negative!", MessageType.Error);
                break;
            case 0:
                EditorGUILayout.HelpBox("Move speed is zero, the player won't move!", MessageType.Warning);
                break;
            case > 10:
                EditorGUILayout.HelpBox("Move speed is quite high!", MessageType.Info);
                break;
            default:
                break;
        }
    }

}
