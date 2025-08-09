using UnityEngine;

public class RoomMeshBuilder : MonoBehaviour
{
    [SerializeField] private RoomMeshGenerator meshGenerator;

    void Start()
    {
        RectInt roomRect = new RectInt(0, 0, 10, 8); // x, y, width, height
        RoomInfo roomInfo = new RoomInfo(null, roomRect);
        
        // Generate the mesh
        GameObject roomMesh = meshGenerator.GenerateRoomMesh(roomInfo);
        
        // Add openings for doors if needed
        meshGenerator.AddOpening(roomMesh, new Vector2Int(5, 0), RoomMeshGenerator.Direction.South);
        meshGenerator.AddOpening(roomMesh, new Vector2Int(10, 4), RoomMeshGenerator.Direction.East);
    }
}