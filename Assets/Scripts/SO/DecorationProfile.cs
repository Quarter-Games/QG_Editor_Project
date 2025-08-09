using UnityEngine;

[CreateAssetMenu(fileName = "DecorationProfile", menuName = "Dungeon/Decoration Profile")]
public class DecorationProfile : ScriptableObject
{
    [System.Serializable]
    public class DecorRule
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float probability = 0.3f;
        public float minSpacing = 2f; // meters between instances
        public string allowedTag = "Floor"; // conceptual tag for tiles/spots
    }

    public DecorRule[] rules;
    [Tooltip("Grid step for décor sampling inside the room.")]
    public float sampleStep = 2f;
}
