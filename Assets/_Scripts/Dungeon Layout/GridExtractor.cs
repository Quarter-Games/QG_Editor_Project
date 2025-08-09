using UnityEngine;

public class GridExtractor : MonoBehaviour
{
    // Reference to the room generator to retrieve space dimensions
    [SerializeField]
    private RoomGenerator roomGenerator;

    // Reference for the cell size of the grid to be created
    [SerializeField]
    private float cellSize = 1f;

    // The grid dimensions (calculated based on room generator's space)
    private Vector2Int gridDimensions;
    
    // Room generator area for gizmo visualization
    private RectInt generatorArea;
    
    // Visualization toggle
    [SerializeField]
    private bool showGridGizmos = true;
    
    // Gizmo color
    [SerializeField]
    private Color gizmoColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);

    private void Start()
    {
        if (roomGenerator == null)
        {
            Debug.LogError("RoomGenerator reference not set! Please assign it in the Inspector.");
            return;
        }
        
        // Create the grid (can be customized as needed)
        CreateGrid();
    }
    [ContextMenu("CreateGrid")]
    public void CreateGrid()
    {
        // Extract the size of the room generator space
        generatorArea = roomGenerator.GetGenerationArea();
        
        // Calculate the grid dimensions based on the room generator's area and desired cell size
        gridDimensions = new Vector2Int(
            Mathf.CeilToInt(generatorArea.width / cellSize),
            Mathf.CeilToInt(generatorArea.height / cellSize)
        );
        
        Debug.Log($"Creating grid with dimensions {gridDimensions.x} x {gridDimensions.y} and cell size {cellSize}");

        // Optionally, visualize the grid or store its data for further processing
        for (int x = 0; x < gridDimensions.x; x++)
        {
            for (int y = 0; y < gridDimensions.y; y++)
            {
                // Here, you can instantiate grid-related objects or simply log the grid creation
                Vector3 cellPosition = new Vector3(x * cellSize, 0, y * cellSize);
                Debug.Log($"Grid cell at {cellPosition}");
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        // Only draw if visualization is enabled and we have valid data
        if (!showGridGizmos || roomGenerator == null)
            return;
            
        // If we haven't calculated the generator area yet (before Start is called)
        if (generatorArea.width == 0 && generatorArea.height == 0)
        {
            generatorArea = roomGenerator.GetGenerationArea();
            gridDimensions = new Vector2Int(
                Mathf.CeilToInt(generatorArea.width / cellSize),
                Mathf.CeilToInt(generatorArea.height / cellSize)
            );
        }
        
        // Set the gizmo color
        Gizmos.color = gizmoColor;
        
        // Draw horizontal grid lines
        for (int y = 0; y <= gridDimensions.y; y++)
        {
            Vector3 startPos = new Vector3(0, 0, y * cellSize);
            Vector3 endPos = new Vector3(gridDimensions.x * cellSize, 0, y * cellSize);
            Gizmos.DrawLine(startPos, endPos);
        }
        
        // Draw vertical grid lines
        for (int x = 0; x <= gridDimensions.x; x++)
        {
            Vector3 startPos = new Vector3(x * cellSize, 0, 0);
            Vector3 endPos = new Vector3(x * cellSize, 0, gridDimensions.y * cellSize);
            Gizmos.DrawLine(startPos, endPos);
        }
    }
}