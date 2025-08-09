using UnityEngine;

public enum RoomKind { Normal, Start, Key, Locked, Quest, Treasure }

[CreateAssetMenu(fileName = "RoomType", menuName = "Dungeon/Room Type")]
public class RoomType : ScriptableObject
{
    public RoomKind kind = RoomKind.Normal;

    [Header("Shape & Size")]
    public Vector2Int sizeMin = new Vector2Int(6, 6);
    public Vector2Int sizeMax = new Vector2Int(12, 12);

    [Tooltip("Desired count range for this room type in the whole dungeon")]
    public Vector2Int countRange = new Vector2Int(1, 3);

    [Header("Placement Rules")]
    public int minFloor = 0;
    public int maxFloor = 99;
    public Color gizmoColor = Color.gray;

    [Header("Quest/Lock")]
    public string keyId; // For Key or Locked types

    [Header("Decoration Profile")]
    public DecorationProfile decoration;
}
