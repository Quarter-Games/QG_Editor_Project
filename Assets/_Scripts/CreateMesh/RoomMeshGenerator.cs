using UnityEngine;

public class RoomMeshGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    [SerializeField] private float floorHeight = 0.0f;
    [SerializeField] private float wallHeight = 2.5f;
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material wallMaterial;

    public GameObject DoorPrefab;
    
    /// <summary>
    /// Generates a mesh for a room based on its RoomInfo.
    /// </summary>
    /// <param name="roomInfo">The RoomInfo containing room dimensions</param>
    /// <returns>A GameObject containing the generated mesh</returns>
    public GameObject GenerateRoomMesh(RoomInfo roomInfo)
    {
        if (roomInfo == null || roomInfo.RoomRect.width <= 0 || roomInfo.RoomRect.height <= 0)
        {
            Debug.LogError("Invalid RoomInfo provided for mesh generation");
            return null;
        }

        // Create a parent GameObject to hold floor and walls
        GameObject roomMeshParent = new GameObject($"Room_Mesh_{roomInfo.RoomRect.x}_{roomInfo.RoomRect.y}");
        
        // Generate floor
        GameObject floorObject = GenerateFloor(roomInfo.RoomRect);
        floorObject.transform.SetParent(roomMeshParent.transform, false);
        
        // Generate walls
        GameObject wallsObject = GenerateWalls(roomInfo.RoomRect);
        wallsObject.transform.SetParent(roomMeshParent.transform, false);
        
        // Position the room mesh at the correct world position
        roomMeshParent.transform.position = new Vector3(
            roomInfo.RoomRect.x, 
            0, 
            roomInfo.RoomRect.y
        );
        
        return roomMeshParent;
    }
    
    private GameObject GenerateFloor(RectInt roomRect)
    {
        GameObject floorObject = new GameObject("Floor");
        MeshFilter meshFilter = floorObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = floorObject.AddComponent<MeshRenderer>();
        
        Mesh mesh = new Mesh();
        
        // Create vertices (corners of the room floor)
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(0, floorHeight, 0), // Bottom left
            new Vector3(roomRect.width, floorHeight, 0), // Bottom right
            new Vector3(0, floorHeight, roomRect.height), // Top left
            new Vector3(roomRect.width, floorHeight, roomRect.height) // Top right
        };
        
        // Create triangles (two triangles to form a quad)
        int[] triangles = new int[6]
        {
            0, 2, 1, // First triangle
            2, 3, 1  // Second triangle
        };
        
        // Create UVs
        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        
        // Calculate normals
        mesh.RecalculateNormals();
        
        // Assign mesh and material
        meshFilter.mesh = mesh;
        meshRenderer.material = floorMaterial;
        
        return floorObject;
    }
    
    private GameObject GenerateWalls(RectInt roomRect)
    {
        GameObject wallsObject = new GameObject("Walls");
        MeshFilter meshFilter = wallsObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = wallsObject.AddComponent<MeshRenderer>();
        
        Mesh mesh = new Mesh();
        
        // 4 walls with 4 vertices each = 16 vertices
        Vector3[] vertices = new Vector3[16];
        
        // South wall (bottom)
        vertices[0] = new Vector3(0, floorHeight, 0);
        vertices[1] = new Vector3(roomRect.width, floorHeight, 0);
        vertices[2] = new Vector3(0, floorHeight + wallHeight, 0);
        vertices[3] = new Vector3(roomRect.width, floorHeight + wallHeight, 0);
        
        // East wall (right)
        vertices[4] = new Vector3(roomRect.width, floorHeight, 0);
        vertices[5] = new Vector3(roomRect.width, floorHeight, roomRect.height);
        vertices[6] = new Vector3(roomRect.width, floorHeight + wallHeight, 0);
        vertices[7] = new Vector3(roomRect.width, floorHeight + wallHeight, roomRect.height);
        
        // North wall (top)
        vertices[8] = new Vector3(roomRect.width, floorHeight, roomRect.height);
        vertices[9] = new Vector3(0, floorHeight, roomRect.height);
        vertices[10] = new Vector3(roomRect.width, floorHeight + wallHeight, roomRect.height);
        vertices[11] = new Vector3(0, floorHeight + wallHeight, roomRect.height);
        
        // West wall (left)
        vertices[12] = new Vector3(0, floorHeight, roomRect.height);
        vertices[13] = new Vector3(0, floorHeight, 0);
        vertices[14] = new Vector3(0, floorHeight + wallHeight, roomRect.height);
        vertices[15] = new Vector3(0, floorHeight + wallHeight, 0);
        
        // 4 walls with 2 triangles each = 24 indices
        int[] triangles = new int[24];
        
        // South wall
        triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
        triangles[3] = 1; triangles[4] = 2; triangles[5] = 3;
        
        // East wall
        triangles[6] = 4; triangles[7] = 6; triangles[8] = 5;
        triangles[9] = 5; triangles[10] = 6; triangles[11] = 7;
        
        // North wall
        triangles[12] = 8; triangles[13] = 10; triangles[14] = 9;
        triangles[15] = 9; triangles[16] = 10; triangles[17] = 11;
        
        // West wall
        triangles[18] = 12; triangles[19] = 14; triangles[20] = 13;
        triangles[21] = 13; triangles[22] = 14; triangles[23] = 15;
        
        // Create UVs based on wall size
        Vector2[] uvs = new Vector2[16];
        for (int i = 0; i < 16; i += 4)
        {
            uvs[i] = new Vector2(0, 0);
            uvs[i + 1] = new Vector2(1, 0);
            uvs[i + 2] = new Vector2(0, 1);
            uvs[i + 3] = new Vector2(1, 1);
        }
        
        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        
        // Calculate normals
        mesh.RecalculateNormals();
        
        // Assign mesh and material
        meshFilter.mesh = mesh;
        meshRenderer.material = wallMaterial;
        
        // Add MeshCollider for physics interactions
        MeshCollider meshCollider = wallsObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        
        return wallsObject;
    }
    
    /// <summary>
    /// Utility method to add openings (doors/passages) to the room mesh
    /// </summary>
    public void AddOpening(GameObject roomMeshObject, Vector2Int position, Direction direction, float width = 1.5f, float height = 2.0f, bool withDoor = false)
    {
        // Implementation would cut holes in the walls for doors/passages
        // This will require to modifying the wall meshes
        
        if (withDoor)
        {
            GameObject doorway = Instantiate(DoorPrefab, roomMeshObject.transform, false);
            doorway.name = "Doorway_"+direction;

            // Position the doorway based on direction and position
            Vector3 doorPosition = Vector3.zero;
            Quaternion doorRotation = Quaternion.identity;

            switch (direction)
            {
                case Direction.North:
                    doorPosition = new Vector3(position.x, floorHeight, position.y);
                    doorRotation = Quaternion.Euler(0, 0, 0);
                    break;
                case Direction.East:
                    doorPosition = new Vector3(position.x, floorHeight, position.y);
                    doorRotation = Quaternion.Euler(0, 90, 0);
                    break;
                case Direction.South:
                    doorPosition = new Vector3(position.x, floorHeight, position.y);
                    doorRotation = Quaternion.Euler(0, 180, 0);
                    break;
                case Direction.West:
                    doorPosition = new Vector3(position.x, floorHeight, position.y);
                    doorRotation = Quaternion.Euler(0, 270, 0);
                    break;
            }

            doorway.transform.position = doorPosition;
            doorway.transform.rotation = doorRotation;
        }
        
    }
    
    public enum Direction
    {
        North,
        East,
        South,
        West
    }
}