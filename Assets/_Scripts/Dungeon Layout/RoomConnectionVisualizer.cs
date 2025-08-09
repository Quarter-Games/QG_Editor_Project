using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper class to visualize room connections
/// </summary>
public class RoomConnectionVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [Tooltip("Color for primary connections")]
    public Color mstConnectionColor = Color.red;
    
    [Tooltip("Color for secondary connections")]
    public Color extraConnectionColor = Color.yellow;
    
    [Tooltip("Whether to show the connections in the scene view")]
    public bool showConnections = true;
    
    private List<(RoomInfo, RoomInfo, bool)> connections = new List<(RoomInfo, RoomInfo, bool)>();
    
    /// <summary>
    /// Sets the connections to visualize
    /// </summary>
    /// <param name="connections">List of room pairs to visualize</param>
    /// <param name="areMstConnections">Whether these are MST connections or additional ones</param>
    public void SetConnections(List<(RoomInfo, RoomInfo)> connections, bool areMstConnections = true)
    {
        // Clear previous connections of this type
        this.connections.RemoveAll(c => c.Item3 == areMstConnections);
        
        // Add new connections
        foreach (var connection in connections)
        {
            this.connections.Add((connection.Item1, connection.Item2, areMstConnections));
        }
    }
    
    /// <summary>
    /// Adds connections to visualize
    /// </summary>
    /// <param name="connections">List of room pairs to visualize</param>
    /// <param name="areMstConnections">Whether these are MST connections or additional ones</param>
    public void AddConnections(List<(RoomInfo, RoomInfo)> connections, bool areMstConnections = true)
    {
        foreach (var connection in connections)
        {
            this.connections.Add((connection.Item1, connection.Item2, areMstConnections));
        }
    }
    
    /// <summary>
    /// Clears all connections
    /// </summary>
    public void ClearConnections()
    {
        connections.Clear();
    }
    
    private void OnDrawGizmos()
    {
        if (!showConnections || connections == null || connections.Count == 0)
            return;
        
        foreach (var connection in connections)
        {
            if (connection.Item1.RoomObject == null || connection.Item2.RoomObject == null)
                continue;
            
           Gizmos.color = connection.Item3 ? mstConnectionColor : extraConnectionColor;
            
            Vector3 startPos = new Vector3(
                connection.Item1.RoomRect.x + connection.Item1.RoomRect.width / 2,
                connection.Item3 ? 0.5f : 0.6f, // MST connections slightly lower
                connection.Item1.RoomRect.y + connection.Item1.RoomRect.height / 2
            );
            
            Vector3 endPos = new Vector3(
                connection.Item2.RoomRect.x + connection.Item2.RoomRect.width / 2,
                connection.Item3 ? 0.5f : 0.6f, // MST connections slightly lower
                connection.Item2.RoomRect.y + connection.Item2.RoomRect.height / 2
            );
            
            Gizmos.DrawLine(startPos, endPos);
        }
    }
}