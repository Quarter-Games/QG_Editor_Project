using System.Collections.Generic;
using UnityEngine;

public class BSPNode
{
    public RectInt area;
    public BSPNode left;
    public BSPNode right;
    public RectInt room;
    public int depth; // Track the depth of each node
    
    // Reference to the seeded random generator
    private System.Random seededRandom;

    private static readonly Color[] depthColors = new Color[]
    {
        new Color(1, 0, 0, 0.3f),     // Red (depth 0)
        new Color(0, 1, 0, 0.3f),     // Green (depth 1)
        new Color(0, 0, 1, 0.3f),     // Blue (depth 2)
        new Color(1, 1, 0, 0.3f),     // Yellow (depth 3)
        new Color(1, 0, 1, 0.3f),     // Magenta (depth 4)
        
        new Color(0, 1, 1, 0.3f),     // Cyan (depth 5)
        new Color(1, 0.5f, 0, 0.3f),  // Orange (depth 6)
        new Color(0.5f, 0, 1, 0.3f)   // Purple (depth 7)
    };

    public BSPNode(int x, int y, int width, int height, int depth = 0, System.Random random = null)
    {
        area = new RectInt(x, y, width, height);
        this.depth = depth;
        this.seededRandom = random;
    }

    public void Split(int depth, int minLeafSize)
    {
        if (depth <= 0) return;

        // Prevent splitting if this leaf is too small
        if (area.width < minLeafSize * 2 && area.height < minLeafSize * 2)
            return;

        bool splitHorizontally;

        // Decide split direction based on current aspect ratio - this can be changed
        if (area.width / (float)area.height >= 1.25f)
            splitHorizontally = false;
        else if (area.height / (float)area.width >= 1.25f)
            splitHorizontally = true;
        else
            splitHorizontally = seededRandom != null ? seededRandom.NextDouble() > 0.5f : Random.value > 0.5f;

        int maxSplit = (splitHorizontally ? area.height : area.width) - minLeafSize;
        if (maxSplit <= minLeafSize)
            return; // Too small to split

        int splitPos = seededRandom != null 
            ? minLeafSize + seededRandom.Next(maxSplit - minLeafSize + 1) 
            : Random.Range(minLeafSize, maxSplit);

        if (splitHorizontally)
        {
            left = new BSPNode(area.x, area.y, area.width, splitPos, this.depth + 1, seededRandom);
            right = new BSPNode(area.x, area.y + splitPos, area.width, area.height - splitPos, this.depth + 1, seededRandom);
        }
        else
        {
            left = new BSPNode(area.x, area.y, splitPos, area.height, this.depth + 1, seededRandom);
            right = new BSPNode(area.x + splitPos, area.y, area.width - splitPos, area.height, this.depth + 1, seededRandom);
        }

        left.Split(depth - 1, minLeafSize);
        right.Split(depth - 1, minLeafSize);
    }

    public List<BSPNode> GetLeafNodes()
    {
        List<BSPNode> leaves = new List<BSPNode>();
        if (left == null && right == null)
            leaves.Add(this);
        else
        {
            if (left != null) leaves.AddRange(left.GetLeafNodes());
            if (right != null) leaves.AddRange(right.GetLeafNodes());
        }
        return leaves;
    }
    
    // Room creation method
    public RoomInfo CreateRoom(GameObject prefab, int floor, int minWidth, int minHeight, int maxWidth, int maxHeight)
    {
        int padding = 2; // Ensures room is not too close to the leaf edges

        // Calculate maximum possible room size within this node (considering padding)
        int maxPossibleWidth = Mathf.Min(area.width - padding * 2, maxWidth);
        int maxPossibleHeight = Mathf.Min(area.height - padding * 2, maxHeight);

        // Ensure minimum room size is possible
        if (maxPossibleWidth < minWidth || maxPossibleHeight < minHeight)
            return null;

        // Generate room dimensions within the constraints
        int roomWidth = seededRandom != null 
            ? minWidth + seededRandom.Next(maxPossibleWidth - minWidth + 1) 
            : Random.Range(minWidth, maxPossibleWidth + 1);
            
        int roomHeight = seededRandom != null 
            ? minHeight + seededRandom.Next(maxPossibleHeight - minHeight + 1) 
            : Random.Range(minHeight, maxPossibleHeight + 1);

        // Position the room within the node area (with padding)
        int maxRoomX = area.width - roomWidth - padding + 1;
        int maxRoomY = area.height - roomHeight - padding + 1;
        
        int roomX = area.x + (seededRandom != null 
            ? padding + seededRandom.Next(maxRoomX - padding + 1) 
            : Random.Range(padding, maxRoomX + 1));
            
        int roomY = area.y + (seededRandom != null 
            ? padding + seededRandom.Next(maxRoomY - padding + 1) 
            : Random.Range(padding, maxRoomY + 1));

        room = new RectInt(roomX, roomY, roomWidth, roomHeight);

        Vector3 pos = new Vector3(room.x + room.width / 2, floor * 5f, room.y + room.height / 2);
        GameObject newRoom = GameObject.Instantiate(prefab, pos, Quaternion.identity);
        newRoom.transform.localScale = new Vector3(room.width, 1, room.height);
        
        // Return both the GameObject and RectInt information
        return new RoomInfo(newRoom, room);
    }

    public Vector2Int GetRoomCenter()
    {
        if (room != null)
            return new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
        else
            return new Vector2Int(area.x + area.width / 2, area.y + area.height / 2);
    }
    
    // Draw this node's area as a gizmo
    public void DrawGizmos(int floor = 0)
    {
        // Get the color for this depth (cycle through colors if we have more depths than colors)
        Color color = depthColors[depth % depthColors.Length];
        
        // Set the gizmo color
        Gizmos.color = color;
        
        // Calculate the world position of the area (accounting for floor height)
        Vector3 position = new Vector3(area.x + area.width / 2f, floor * 5f, area.y + area.height / 2f);
        Vector3 size = new Vector3(area.width, 0.1f, area.height);
        
        // Draw the wire cube to represent the area
        Gizmos.DrawWireCube(position, size);
        
        // Draw the room if it exists
        if (room != null)
        {
            // Use a slightly lighter color for the room
            Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
            Vector3 roomPos = new Vector3(room.x + room.width / 2f, floor * 5f, room.y + room.height / 2f);
            Vector3 roomSize = new Vector3(room.width, 0.2f, room.height);
            Gizmos.DrawCube(roomPos, roomSize);
        }
        
        // Draw the child nodes recursively
        if (left != null) left.DrawGizmos(floor);
        if (right != null) right.DrawGizmos(floor);
    }
}