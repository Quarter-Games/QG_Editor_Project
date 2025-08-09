using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Creates connections between rooms using Delaunay triangulation followed by MST
/// </summary>
public class DelaunayConnectionGenerator
{
    // Configuration parameters
    private int neighborCount = 3; 
    private float extraConnectionChance = 1f;
    
    public DelaunayConnectionGenerator(int neighborCount = 3, float extraConnectionChance = 1f)
    {
        this.neighborCount = Mathf.Clamp(neighborCount, 1, 5);
        this.extraConnectionChance = Mathf.Clamp01(extraConnectionChance);
    }
    
    /// <summary>
    /// Generates connections between rooms using Delaunay triangulation and MST
    /// </summary>
    /// <param name="roomInfos">List of rooms to connect</param>
    /// <returns>List of room pairs that should be connected</returns>
    public List<(RoomInfo, RoomInfo)> GenerateConnections(List<RoomInfo> roomInfos)
    {
        if (roomInfos == null || roomInfos.Count <= 1)
            return new List<(RoomInfo, RoomInfo)>();
        
        // Step 1: Generate Delaunay-like triangulation
        List<(RoomInfo, RoomInfo)> delaunayConnections = GenerateDelaunayConnections(roomInfos);
        
        // Step 2: Apply MST to ensure connectivity
        List<(RoomInfo, RoomInfo)> mstConnections = GenerateMSTFromDelaunay(roomInfos, delaunayConnections);
        
        // Step 3: Add some extra connections for more interesting layouts
        List<(RoomInfo, RoomInfo)> finalConnections = AddExtraConnections(mstConnections, delaunayConnections);
        
        return finalConnections;
    }
    
    /// <summary>
    /// Creates a Delaunay-like triangulation by connecting rooms to their nearest neighbors
    /// </summary>
    private List<(RoomInfo, RoomInfo)> GenerateDelaunayConnections(List<RoomInfo> roomInfos)
    {
        List<(RoomInfo, RoomInfo)> connections = new List<(RoomInfo, RoomInfo)>();
        
        // Connect each room to its N nearest neighbors
        for (int i = 0; i < roomInfos.Count; i++)
        {
            // Get distances to all other rooms
            List<(int index, float distance)> roomsByDistance = new List<(int, float)>();
            
            for (int j = 0; j < roomInfos.Count; j++)
            {
                if (i != j)
                {
                    Vector2 centerA = GetRoomCenter(roomInfos[i]);
                    Vector2 centerB = GetRoomCenter(roomInfos[j]);
                    float distance = Vector2.Distance(centerA, centerB);
                    roomsByDistance.Add((j, distance));
                }
            }
            
            // Sort by distance
            roomsByDistance.Sort((a, b) => a.distance.CompareTo(b.distance));
            
            // Connect to the N closest rooms
            int connectionsToMake = Mathf.Min(neighborCount, roomsByDistance.Count);
            for (int k = 0; k < connectionsToMake; k++)
            {
                int otherRoomIndex = roomsByDistance[k].index;
                
                // Avoid duplicate connections
                bool connectionExists = connections.Any(c => 
                    (c.Item1 == roomInfos[i] && c.Item2 == roomInfos[otherRoomIndex]) || 
                    (c.Item1 == roomInfos[otherRoomIndex] && c.Item2 == roomInfos[i]));
                    
                if (!connectionExists)
                {
                    connections.Add((roomInfos[i], roomInfos[otherRoomIndex]));
                }
            }
        }
        
        return connections;
    }
    
    /// <summary>
    /// Applies MST algorithm to ensure minimum connectivity
    /// </summary>
    private List<(RoomInfo, RoomInfo)> GenerateMSTFromDelaunay(
        List<RoomInfo> roomInfos, 
        List<(RoomInfo, RoomInfo)> delaunayConnections)
    {
        List<(RoomInfo, RoomInfo)> mstConnections = new List<(RoomInfo, RoomInfo)>();
        
        // Convert connections to edges with distances
        List<(RoomInfo roomA, RoomInfo roomB, float distance)> edges = new List<(RoomInfo, RoomInfo, float)>();
        
        foreach (var connection in delaunayConnections)
        {
            Vector2 centerA = GetRoomCenter(connection.Item1);
            Vector2 centerB = GetRoomCenter(connection.Item2);
            float distance = Vector2.Distance(centerA, centerB);
            
            edges.Add((connection.Item1, connection.Item2, distance));
        }
        
        // Sort edges by distance
        edges.Sort((a, b) => a.distance.CompareTo(b.distance));
        
        // Apply Kruskal's algorithm
        Dictionary<RoomInfo, RoomInfo> parent = new Dictionary<RoomInfo, RoomInfo>();
        foreach (var room in roomInfos)
        {
            parent[room] = room;
        }
        
        // Find with path compression
        RoomInfo Find(RoomInfo room)
        {
            if (parent[room] != room)
                parent[room] = Find(parent[room]);
            return parent[room];
        }
        
        // Union
        void Union(RoomInfo a, RoomInfo b)
        {
            parent[Find(a)] = Find(b);
        }
        
        // Build the MST
        foreach (var edge in edges)
        {
            RoomInfo rootA = Find(edge.roomA);
            RoomInfo rootB = Find(edge.roomB);
            
            if (rootA != rootB)
            {
                mstConnections.Add((edge.roomA, edge.roomB));
                Union(edge.roomA, edge.roomB);
            }
        }
        
        return mstConnections;
    }
    
    /// <summary>
    /// Adds some extra connections from the Delaunay triangulation to create loops
    /// </summary>
    private List<(RoomInfo, RoomInfo)> AddExtraConnections(
        List<(RoomInfo, RoomInfo)> mstConnections, 
        List<(RoomInfo, RoomInfo)> delaunayConnections)
    {
        List<(RoomInfo, RoomInfo)> finalConnections = new List<(RoomInfo, RoomInfo)>(mstConnections);
        
        // Find connections that are in Delaunay but not in MST
        List<(RoomInfo, RoomInfo)> extraConnections = new List<(RoomInfo, RoomInfo)>();
        
        foreach (var connection in delaunayConnections)
        {
            bool inMST = mstConnections.Any(c => 
                (c.Item1 == connection.Item1 && c.Item2 == connection.Item2) || 
                (c.Item1 == connection.Item2 && c.Item2 == connection.Item1));
                
            if (!inMST)
            {
                extraConnections.Add(connection);
            }
        }
        
        // Randomly add some of these extra connections
        System.Random random = new System.Random();
        extraConnections = extraConnections.OrderBy(x => random.Next()).ToList();
        
        int extraToAdd = Mathf.FloorToInt(mstConnections.Count * extraConnectionChance);
        for (int i = 0; i < Mathf.Min(extraToAdd, extraConnections.Count); i++)
        {
            finalConnections.Add(extraConnections[i]);
        }
        
        return finalConnections;
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
}