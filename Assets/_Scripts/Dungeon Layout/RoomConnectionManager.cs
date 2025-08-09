using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonoBehaviour that visualizes connections between rooms using different algorithms
/// </summary>
public class RoomConnectionManager : MonoBehaviour
{
    [Header("Connection Settings")]
    [Tooltip("The connection algorithm to use")]
    public ConnectionGeneratorFactory.ConnectionType connectionType = ConnectionGeneratorFactory.ConnectionType.MST;
    
    [Tooltip("Number of neighbors to consider for Delaunay connections")]
    [Range(1, 10)]
    public int neighborCount = 3;
    
    // Reference to the room generator
    private RoomGenerator roomGenerator;
    
    // The visualizer component
    private RoomConnectionVisualizer visualizer;
    
    private void Awake()
    {
        // Get or add the required components
        var roomGeneratorObj = FindObjectOfType<RoomGenerator>();
        
        if (roomGeneratorObj!=null && roomGeneratorObj.gameObject == gameObject)
            roomGenerator = roomGeneratorObj;

        if (roomGenerator == null)
        {
            Debug.LogError("RoomConnectionManager requires a RoomGenerator component!");
            enabled = false;
            return;
        }
        
        visualizer = GetComponent<RoomConnectionVisualizer>();
        if (visualizer == null)
        {
            visualizer = gameObject.AddComponent<RoomConnectionVisualizer>();
        }
    }
    
    private void Start()
    {
        // If rooms were already generated, visualize connections
        if (roomGenerator.GetRoomInfos().Count > 0)
        {
            VisualizeConnections();
        }
    }
    
    /// <summary>
    /// Generates and visualizes connections between rooms
    /// </summary>
    [ContextMenu("Visualize Connections")]
    public void VisualizeConnections()
    {
        if (roomGenerator == null)
        {
            Debug.LogError("RoomGenerator is not assigned!");
            return;
        }
        
        List<RoomInfo> rooms = roomGenerator.GetRoomInfos();
        if (rooms.Count < 2)
        {
            Debug.LogWarning("Not enough rooms to create connections!");
            return;
        }
        
        // Clear previous connections
        visualizer.ClearConnections();
        
        // Get BSP root node if needed for BSP connections
        BSPNode rootNode = connectionType == ConnectionGeneratorFactory.ConnectionType.BSP 
            ? roomGenerator.GetBSPRootNode() // Assuming this method exists
            : null;
        
        // Generate connections based on selected algorithm
        // Always use 1.0 (100%) for extra connection chance to maximize connections
        List<(RoomInfo, RoomInfo)> connections = ConnectionGeneratorFactory.CreateConnections(
            connectionType, 
            rooms, 
            rootNode,
            neighborCount, 
            1.0f
        );
        
        // Visualize the connections
        visualizer.SetConnections(connections, true);
        
        Debug.Log($"Generated {connections.Count} connections using {connectionType} algorithm");
    }
    
    // Refresh connections when rooms are generated
    public void OnRoomsGenerated()
    {
        VisualizeConnections();
    }
}