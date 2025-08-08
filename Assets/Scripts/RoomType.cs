using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/RoomType")]
public class RoomType : ScriptableObject
{
    public string roomName;
    public RoomType type;
    public Vector2Int sizeMin;
    public Vector2Int sizeMax;
    public RoomShape shape;
    public int minCount;
    public int maxCount;
    public Color gizmoColor;

    [Header("Placement Rules")]
    public bool mustBeOnStartFloor;
    public bool mustBeAccessibleFromStart;
    public List<RoomType> mustNotBeNear;
    public int minDistanceFromSameType = 0;
}
