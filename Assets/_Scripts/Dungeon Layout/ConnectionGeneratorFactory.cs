using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Factory for creating different types of connection generators
/// </summary>
public static class ConnectionGeneratorFactory
{
    public enum ConnectionType
    {
        MST,
        Delaunay,
        BSP
    }
    
    /// <summary>
    /// Creates connections between rooms using the specified algorithm
    /// </summary>
    public static List<(RoomInfo, RoomInfo)> CreateConnections(
        ConnectionType type, 
        List<RoomInfo> roomInfos, 
        BSPNode rootNode = null,
        int neighborCount = 3, 
        float extraConnectionChance = 0.2f)
    {
        switch (type)
        {
            case ConnectionType.MST:
                MSTConnectionGenerator mstGen = new MSTConnectionGenerator();
                return mstGen.GenerateConnections(roomInfos);
                
            case ConnectionType.Delaunay:
                DelaunayConnectionGenerator delaunayGen = new DelaunayConnectionGenerator(neighborCount, extraConnectionChance);
                return delaunayGen.GenerateConnections(roomInfos);
                
            case ConnectionType.BSP:
                if (rootNode == null)
                {
                    Debug.LogError("BSP connection generation requires a valid rootNode!");
                    return new List<(RoomInfo, RoomInfo)>();
                }
                
                // Implementation of BSP-based connection generation would go here
                // (you could create a separate BSPConnectionGenerator class)
                return new List<(RoomInfo, RoomInfo)>();
                
            default:
                return new List<(RoomInfo, RoomInfo)>();
        }
    }
}