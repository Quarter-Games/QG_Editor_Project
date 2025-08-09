
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extensions for the RoomGenerator class to work with connection generators
/// </summary>
public static class RoomGeneratorExtensions
{
    /// <summary>
    /// Creates corridors between rooms based on the specified connections
    /// </summary>
    public static void CreateCorridorsFromConnections(this RoomGenerator generator, 
        List<(RoomInfo, RoomInfo)> connections, GameObject corridorPrefab)
    {
        if (corridorPrefab == null)
        {
            Debug.LogError("Corridor prefab is not assigned!");
            return;
        }
        
        CorridorGenerator corridorGen = new CorridorGenerator();
        
        foreach (var connection in connections)
        {
            Vector2Int startCenter = new Vector2Int(
                connection.Item1.RoomRect.x + connection.Item1.RoomRect.width / 2,
                connection.Item1.RoomRect.y + connection.Item1.RoomRect.height / 2
            );
            
            Vector2Int endCenter = new Vector2Int(
                connection.Item2.RoomRect.x + connection.Item2.RoomRect.width / 2,
                connection.Item2.RoomRect.y + connection.Item2.RoomRect.height / 2
            );
            
            // Create L-shaped corridor between rooms
            corridorGen.CreateLShapedCorridor(startCenter, endCenter, corridorPrefab, 0);
        }
    }
}