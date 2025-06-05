using UnityEditor;
using UnityEngine;

[System.Serializable]
public class SineWave
{
    public float Amplitude;
    public float Frequency;
    public float Phase;
    public float WaveLength;
}

[CustomPropertyDrawer(typeof(SineWave))]
public class SineWaveProperty : PropertyDrawer
{
    public SineWaveProperty()
    {

    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {

        return 300;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Calculate rects
        float fieldHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;
        float graphHeight = position.height - (fieldHeight + spacing) * 4 - spacing * 2;
        float graphWidth = position.width * 0.6f;
        float fieldWidth = (position.width - graphWidth - spacing * 2);

        Rect graphRect = new Rect(position.x, position.y, graphWidth, graphHeight);
        Rect ampRect = new Rect(position.x + graphWidth + spacing, position.y, fieldWidth, fieldHeight);
        Rect freqRect = new Rect(position.x + graphWidth + spacing, position.y + fieldHeight + spacing, fieldWidth, fieldHeight);
        Rect phaseRect = new Rect(position.x + graphWidth + spacing, position.y + (fieldHeight + spacing) * 2, fieldWidth, fieldHeight);
        Rect waveLenRect = new Rect(position.x + graphWidth + spacing, position.y + (fieldHeight + spacing) * 3, fieldWidth, fieldHeight);

        // Draw fields
        EditorGUI.PropertyField(ampRect, property.FindPropertyRelative("Amplitude"), new GUIContent("Amplitude"));
        EditorGUI.PropertyField(freqRect, property.FindPropertyRelative("Frequency"), new GUIContent("Frequency"));
        EditorGUI.PropertyField(phaseRect, property.FindPropertyRelative("Phase"), new GUIContent("Phase"));
        EditorGUI.PropertyField(waveLenRect, property.FindPropertyRelative("WaveLength"), new GUIContent("WaveLength"));

        // Draw sine wave graph
        Handles.BeginGUI();
        Color prevColor = Handles.color;
        Handles.color = Color.cyan;

        float amplitude = property.FindPropertyRelative("Amplitude").floatValue;
        float frequency = property.FindPropertyRelative("Frequency").floatValue;
        float phase = property.FindPropertyRelative("Phase").floatValue;
        float waveLength = property.FindPropertyRelative("WaveLength").floatValue;
        if (waveLength == 0) waveLength = 1f;

        int points = Mathf.Max(2, (int)graphRect.width);
        Vector3[] wavePoints = new Vector3[points];
        for (int i = 0; i < points; i++)
        {
            float t = (float)i / (points - 1);
            float x = graphRect.x + t * graphRect.width;
            float yNorm = Mathf.Sin((t * waveLength * frequency + phase) * Mathf.PI * 2f);
            float y = graphRect.y + graphRect.height / 2f - yNorm * amplitude * (graphRect.height / 2f - 2);
            wavePoints[i] = new Vector3(x, y, 0);
        }
        Handles.DrawAAPolyLine(2f, wavePoints);

        Handles.color = prevColor;
        Handles.EndGUI();

        EditorGUI.EndProperty();
    }
}
