using UnityEngine;

public class RangeToggleAttribute : PropertyAttribute
{
    public string toggleField;
    public RangeToggleAttribute(string toggleField) => this.toggleField = toggleField;
}

public class ImagePreviewAttribute : PropertyAttribute { }

public class MyComponent : MonoBehaviour
{
    public bool showAdvanced;

    [RangeToggle("showAdvanced")]
    public float speed = 5f;

    [ImagePreview]
    public Texture2D icon;
}

[CreateAssetMenu(menuName = "Custom/My Scriptable Data")]
public class MyScriptableData : ScriptableObject
{
    public bool enableExtraSettings;

    [RangeToggle("enableExtraSettings")]
    public int powerLevel = 10;

    [ImagePreview]
    public Texture2D previewImage;
}
