using UnityEditor;
using UnityEngine;

// Drawer for RangeToggle
[CustomPropertyDrawer(typeof(RangeToggleAttribute))]
public class RangeToggleDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var toggle = (RangeToggleAttribute)attribute;
        SerializedProperty toggleProp = property.serializedObject.FindProperty(toggle.toggleField);

        if (toggleProp != null && toggleProp.boolValue)
        {
            EditorGUI.PropertyField(position, property, label);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var toggle = (RangeToggleAttribute)attribute;
        SerializedProperty toggleProp = property.serializedObject.FindProperty(toggle.toggleField);

        return (toggleProp != null && toggleProp.boolValue)
            ? EditorGUI.GetPropertyHeight(property, label, true)
            : 0f;
    }
}

// Drawer for ImagePreview
[CustomPropertyDrawer(typeof(ImagePreviewAttribute))]
public class ImagePreviewDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label);
        Texture2D texture = property.objectReferenceValue as Texture2D;

        if (texture != null)
        {
            Rect previewRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, 64, 64);
            GUI.DrawTexture(previewRect, texture, ScaleMode.ScaleToFit);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        Texture2D texture = property.objectReferenceValue as Texture2D;
        return EditorGUIUtility.singleLineHeight + (texture ? 68 : 0);
    }
}
