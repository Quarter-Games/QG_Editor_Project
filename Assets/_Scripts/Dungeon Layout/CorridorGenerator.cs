using UnityEngine;
// old concept not needed for class
public class CorridorGenerator
{
    // Create corridors between rooms in a BSP tree
    public void CreateCorridors(BSPNode rootNode, GameObject prefab, int floor)
    {
        if (rootNode.left != null && rootNode.right != null)
        {
            Vector2Int leftCenter = rootNode.left.GetRoomCenter();
            Vector2Int rightCenter = rootNode.right.GetRoomCenter();

            // Create L-shaped corridor
            CreateLShapedCorridor(leftCenter, rightCenter, prefab, floor);

            // Recursively create corridors for the children
            CreateCorridors(rootNode.left, prefab, floor);
            CreateCorridors(rootNode.right, prefab, floor);
        }
    }

    public void CreateLShapedCorridor(Vector2Int start, Vector2Int end, GameObject prefab, int floor)
    {
        float floorHeight = floor * 5f;
        
        // Determine if we should go horizontally first, then vertically
        bool horizontalFirst = Random.value > 0.5f;
        
        if (horizontalFirst)
        {
            // First horizontal section
            CreateCorridorSection(
                new Vector3(start.x, floorHeight, start.y),
                new Vector3(end.x, floorHeight, start.y),
                prefab
            );
        
            // Then vertical section
            CreateCorridorSection(
                new Vector3(end.x, floorHeight, start.y),
                new Vector3(end.x, floorHeight, end.y),
                prefab
            );
        }
        else
        {
            // First vertical section
            CreateCorridorSection(
                new Vector3(start.x, floorHeight, start.y),
                new Vector3(start.x, floorHeight, end.y),
                prefab
            );
        
            // Then horizontal section
            CreateCorridorSection(
                new Vector3(start.x, floorHeight, end.y),
                new Vector3(end.x, floorHeight, end.y),
                prefab
            );
        }
    }

    private void CreateCorridorSection(Vector3 start, Vector3 end, GameObject prefab)
    {
        // Skip if the corridor section has zero length
        if (Vector3.Distance(start, end) < 0.1f) return;
        
        Vector3 direction = end - start;
        float length = direction.magnitude;
        
        // Position at the middle of the corridor
        Vector3 position = (start + end) / 2;
        
        // Create the corridor
        GameObject corridor = GameObject.Instantiate(prefab, position, Quaternion.identity);
   
        // Determine which axis this corridor runs along and scale accordingly
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            // Horizontal corridor (X-axis)
            corridor.transform.localScale = new Vector3(length, 1, 1);
        }
        else
        {
            // Vertical corridor (Z-axis)
            corridor.transform.localScale = new Vector3(1, 1, length);
        }
    }
}