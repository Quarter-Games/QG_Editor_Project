using System.Collections.Generic;
using UnityEngine;

public class NoiseCutoutRoomGenerator : RoomGenerator
{
    [Header("Noise Settings")]
    public float noiseScale = 5f;
    public float noiseThreshold = 0.1f;
    public float seedOffsetX = 100f;
    public float seedOffsetY = 100f;

    
    public float tileSize = 1.0f;
    
    protected override void CreateRooms()
    {
        // Clear the previous room info list if we're regenerating
        roomInfos.Clear();
        
        foreach (var node in leafNodes)
        {
            // First create the basic room using the parent's method
            RoomInfo roomInfo = node.CreateRoom(roomPrefab, 0, minRoomWidth, minRoomHeight, maxRoomWidth, maxRoomHeight);
            
            if (roomInfo != null)
            {
                // Apply noise cutout to shape the room
                ApplyNoiseCutout(roomInfo);
                
                roomInfos.Add(roomInfo);
            }
        }
    }

    private void ApplyNoiseCutout(RoomInfo roomInfo)
    {
        // Get the room dimensions
        int roomWidth = roomInfo.RoomRect.width;
        int roomHeight = roomInfo.RoomRect.height;
        
        // Generate noise-based room layout
        bool[,] roomGrid = GenerateRoomWithNoiseCutout(roomWidth, roomHeight);
        
        // Store the generated grid in the room info (you'll need to add this property to RoomInfo)
        roomInfo.SetTileGrid(roomGrid);
        PrintRoomToConsole(roomGrid);
        // At this point, we're not generating floor mesh as specified
        // This will be implemented in a later method
        GenerateMeshFromRoomData(roomInfo.RoomObject, roomGrid);
    }

    /// <summary>
    /// Generate a room grid and remove parts using noise.
    /// </summary>
    private bool[,] GenerateRoomWithNoiseCutout(int w, int h)
    {
        bool[,] grid = new bool[w, h];
        float seedX = Random.Range(0f, 9999f) + seedOffsetX;
        float seedY = Random.Range(0f, 9999f) + seedOffsetY;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                // Generate Perlin noise value
                float noiseValue = Mathf.PerlinNoise((x + seedX) / noiseScale, (y + seedY) / noiseScale);
                grid[x, y] = noiseValue > noiseThreshold; // true = keep, false = cut out
            }
        }

        return grid;
    }
    
    
    void GenerateMeshFromRoomData(GameObject roomGameObject, bool[,] layout)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int w = layout.GetLength(0);
        int h = layout.GetLength(1);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (layout[x, y])
                {
                    // Add quad at position x, y
                    int index = vertices.Count;

                    vertices.Add(new Vector3(x * tileSize, 0, y * tileSize));
                    vertices.Add(new Vector3((x + 1) * tileSize, 0, y * tileSize));
                    vertices.Add(new Vector3((x + 1) * tileSize, 0, (y + 1) * tileSize));
                    vertices.Add(new Vector3(x * tileSize, 0, (y + 1) * tileSize));

                    triangles.Add(index + 0);
                    triangles.Add(index + 2);
                    triangles.Add(index + 1);

                    triangles.Add(index + 0);
                    triangles.Add(index + 3);
                    triangles.Add(index + 2);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        
        roomGameObject.GetComponent<MeshFilter>().mesh = mesh;
    }
    
    
    private void PrintRoomToConsole(bool[,] grid)
    {
        string output = "";
        output += "room size is " + grid.GetLength(0) + " X " + grid.GetLength(1) + "\n";
        for (int y = grid.GetLength(1) - 1; y >= 0; y--) // Print top to bottom
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                output += grid[x, y] ? "#" : "."; // '#' = tile, '.' = empty
            }
            output += "\n";
        }

        Debug.Log(output);
       
    }
    
    
    
}