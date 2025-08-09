using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates connections between rooms using a Minimum Spanning Tree algorithm
/// </summary>
public class MSTConnectionGenerator
{
    /// <summary>
    /// Generates connections between rooms using Minimum Spanning Tree algorithm
    /// </summary>
    /// <param name="roomInfos">List of rooms to connect</param>
    /// <returns>List of room pairs that should be connected</returns>
    public List<(RoomInfo, RoomInfo)> GenerateConnections(List<RoomInfo> roomInfos)
    {
        List<(RoomInfo, RoomInfo)> mstConnections = new List<(RoomInfo, RoomInfo)>();
        
        if (roomInfos == null || roomInfos.Count <= 1)
            return mstConnections;
        
        // Create a list of all possible connections between rooms
        List<(RoomInfo roomA, RoomInfo roomB, float distance)> allPossibleConnections = new List<(RoomInfo, RoomInfo, float)>();
        
        // Calculate distances between all pairs of rooms
        for (int i = 0; i < roomInfos.Count - 1; i++)
        {
            for (int j = i + 1; j < roomInfos.Count; j++)
            {
                Vector2 centerA = GetRoomCenter(roomInfos[i]);
                Vector2 centerB = GetRoomCenter(roomInfos[j]);
                
                float distance = Vector2.Distance(centerA, centerB);
                allPossibleConnections.Add((roomInfos[i], roomInfos[j], distance));
            }
        }
        
        // Sort connections by distance (shortest first)
        allPossibleConnections.Sort((a, b) => a.distance.CompareTo(b.distance));
        
        // Use Kruskal's algorithm to build the MST
        Dictionary<RoomInfo, RoomInfo> parent = new Dictionary<RoomInfo, RoomInfo>();
        foreach (var room in roomInfos)
        {
            parent[room] = room; // Each room starts in its own set
        }
        
        // Find the root of a set (with path compression)
        RoomInfo Find(RoomInfo room)
        {
            if (parent[room] != room)
                parent[room] = Find(parent[room]);
            return parent[room];
        }
        
        // Union two sets
        void Union(RoomInfo a, RoomInfo b)
        {
            parent[Find(a)] = Find(b);
        }
        
        // Add connections to the MST
        foreach (var connection in allPossibleConnections)
        {
            RoomInfo rootA = Find(connection.roomA);
            RoomInfo rootB = Find(connection.roomB);
            
            // If they're not already connected, add this connection
            if (rootA != rootB)
            {
                mstConnections.Add((connection.roomA, connection.roomB));
                Union(connection.roomA, connection.roomB);
            }
        }
        
        return mstConnections;
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