using UnityEngine;
using System.Collections.Generic;
using System;

public class RoomGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapWidth = 50;
    public int mapHeight = 50;
    public int maxDepth = 4;
    public int minLeafSize = 10;
    public GameObject roomPrefab;
    
    [Header("Room Size Settings")]
    [Min(3)]
    public int minRoomWidth = 4;
    [Min(3)]
    public int minRoomHeight = 4;
    [Min(4)]
    public int maxRoomWidth = 12;
    [Min(4)]
    public int maxRoomHeight = 12;

    [Header("Randomness setting")]
    public int RandomSeed = -1;
    protected System.Random seededRandom;
    
    [Header("Visualization")]
    public bool showGizmos = true;
    
    protected List<BSPNode> leafNodes;
    protected BSPNode rootNode;
    // Add this to the class fields
    protected List<RoomInfo> roomInfos = new List<RoomInfo>();

    protected virtual void Start()
    {
        GenerateRooms();
    }

    [ContextMenu("Generate Rooms")]
    public virtual void GenerateRooms()
    {
        // Initialize seeded random with the seed value
        InitializeSeededRandom();
        
        // Clear any existing rooms
        ClearExistingRooms();
        
        // Check if room prefab is assigned
        if (roomPrefab == null)
        {
            Debug.LogError("Room prefab is not assigned. Please assign a prefab in the inspector.");
            return;
        }
        
        // Validate room size parameters
        ValidateRoomSizeParameters();
        
        // Create root node and pass the seeded random
        rootNode = new BSPNode(0, 0, mapWidth, mapHeight, 0, seededRandom);
        rootNode.Split(maxDepth, minLeafSize);

        // Get leaf nodes and create rooms
        leafNodes = rootNode.GetLeafNodes();
        
        // Generate rooms
        CreateRooms();
        
        // Call the method that will be implemented by child classes
        OnRoomsGenerated();
    }
    
    protected void InitializeSeededRandom()
    {
        // Create a new seed if the value is -1
        if (RandomSeed == -1)
        {
            RandomSeed = Environment.TickCount;
        }
        
        seededRandom = new System.Random(RandomSeed);
    }
    
    protected virtual void CreateRooms()
    {
        // Clear the previous room info list if we're regenerating
        roomInfos.Clear();
        
        foreach (var node in leafNodes)
        {
            RoomInfo roomInfo = node.CreateRoom(roomPrefab, 0, minRoomWidth, minRoomHeight, maxRoomWidth, maxRoomHeight);
            if (roomInfo != null)
            {
                roomInfos.Add(roomInfo);
            }
        }
    }
    
    // Changed from abstract to virtual with empty implementation
    protected virtual void OnRoomsGenerated()
    {
        // Empty default implementation
    }
    
    protected void ValidateRoomSizeParameters()
    {
        // Ensure min values are at least 3 (smallest reasonable room)
        minRoomWidth = Mathf.Max(3, minRoomWidth);
        minRoomHeight = Mathf.Max(3, minRoomHeight);
        
        // Ensure max values are greater than min values
        maxRoomWidth = Mathf.Max(minRoomWidth + 1, maxRoomWidth);
        maxRoomHeight = Mathf.Max(minRoomHeight + 1, maxRoomHeight);
    }
    
    protected virtual void ClearExistingRooms()
    {
        // Find and destroy all GameObjects with "Room" tag in the scene
        GameObject[] rooms = GameObject.FindGameObjectsWithTag("Room");
        foreach (GameObject room in rooms)
        {
            #if UNITY_EDITOR
            if (Application.isEditor && !Application.isPlaying)
            {
                DestroyImmediate(room);
            }
            else
            #endif
            {
                Destroy(room);
            }
        }
    }
    
    // Draw gizmos for visualization
    protected virtual void OnDrawGizmos()
    {
        if (!showGizmos || rootNode == null) return;
        
        rootNode.DrawGizmos();
    }
    
    // Add a method to access the room information
    public List<RoomInfo> GetRoomInfos()
    {
        return roomInfos;
    }
    
    // Get the seeded random generator
    public System.Random GetSeededRandom()
    {
        if (seededRandom == null)
        {
            InitializeSeededRandom();
        }
        return seededRandom;
    }

    
    /// <summary>
    /// Determines optimal room connections using the BSP tree structure
    /// </summary>
    public List<(RoomInfo, RoomInfo)> FindConnectionsInBSP()
    {
        List<(RoomInfo, RoomInfo)> connections = new List<(RoomInfo, RoomInfo)>();
        
        if (rootNode == null || roomInfos.Count <= 1)
            return connections;
        
        // Create a mapping from room rects to room info objects
        Dictionary<RectInt, RoomInfo> roomMapping = new Dictionary<RectInt, RoomInfo>();
        foreach (var roomInfo in roomInfos)
        {
            roomMapping[roomInfo.RoomRect] = roomInfo;
        }
        
        // Process all internal nodes of the BSP tree to find connections
        ProcessBSPNode(rootNode, roomMapping, connections);
        
        return connections;
    }

    /// <summary>
    /// Processes a BSP node to find connections between rooms in its subtrees
    /// </summary>
    private void ProcessBSPNode(BSPNode node, Dictionary<RectInt, RoomInfo> roomMapping, 
                              List<(RoomInfo, RoomInfo)> connections)
    {
        // Skip leaf nodes
        if (node.left == null || node.right == null)
            return;
        
        // Get all rooms in left and right subtrees
        List<RoomInfo> leftRooms = GetRoomsInSubtree(node.left, roomMapping);
        List<RoomInfo> rightRooms = GetRoomsInSubtree(node.right, roomMapping);
        
        // If both subtrees have rooms, connect the closest pair
        if (leftRooms.Count > 0 && rightRooms.Count > 0)
        {
            // Find the closest pair of rooms between the subtrees
            ConnectClosestRooms(leftRooms, rightRooms, connections);
        }
        
        // Continue processing child nodes recursively
        ProcessBSPNode(node.left, roomMapping, connections);
        ProcessBSPNode(node.right, roomMapping, connections);
    }

    /// <summary>
    /// Finds all rooms in a BSP subtree
    /// </summary>
    private List<RoomInfo> GetRoomsInSubtree(BSPNode node, Dictionary<RectInt, RoomInfo> roomMapping)
    {
        List<RoomInfo> rooms = new List<RoomInfo>();
        
        // If this is a leaf with a room, add it
        if (node.left == null && node.right == null && node.room.width > 0 && node.room.height > 0)
        {
            if (roomMapping.ContainsKey(node.room))
            {
                rooms.Add(roomMapping[node.room]);
            }
            return rooms;
        }
        
        // Otherwise, recursively collect rooms from children
        if (node.left != null)
        {
            rooms.AddRange(GetRoomsInSubtree(node.left, roomMapping));
        }
        
        if (node.right != null)
        {
            rooms.AddRange(GetRoomsInSubtree(node.right, roomMapping));
        }
        
        return rooms;
    }

    /// <summary>
    /// Connects the closest rooms between two groups
    /// </summary>
    private void ConnectClosestRooms(List<RoomInfo> groupA, List<RoomInfo> groupB, 
                                   List<(RoomInfo, RoomInfo)> connections)
    {
        RoomInfo closestA = null;
        RoomInfo closestB = null;
        float minDistance = float.MaxValue;
        
        foreach (var roomA in groupA)
        {
            foreach (var roomB in groupB)
            {
                Vector2 centerA = GetRoomCenter(roomA);
                Vector2 centerB = GetRoomCenter(roomB);
                
                float distance = Vector2.Distance(centerA, centerB);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestA = roomA;
                    closestB = roomB;
                }
            }
        }
        
        // Add the connection between the closest rooms
        if (closestA != null && closestB != null)
        {
            connections.Add((closestA, closestB));
        }
    }

    /// <summary>
    /// Gets the center position of a room
    /// </summary>
    private Vector2 GetRoomCenter(RoomInfo room)
    {
        return new Vector2(
            room.RoomRect.x + room.RoomRect.width / 2,
            room.RoomRect.y + room.RoomRect.height / 2
        );
    }

    public BSPNode GetBSPRootNode()
    {
        return rootNode;
    }

    public RectInt GetGenerationArea()
    {
        return new RectInt(0, 0, mapWidth, mapHeight);
    }
}