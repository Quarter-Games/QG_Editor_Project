using System.Collections.Generic;
using UnityEngine;

public class WFC_Processor : MonoBehaviour
{
    public int gridWidth = 5;
    public int gridHeight = 5;
    public float cellSize = 1.1f; // Added configurable cell size parameter
    public List<WFC_Tile> tileSet;

    private WFC_Grid grid;
    
    [ContextMenu("Generate")]
    void start()
    {
        grid = new WFC_Grid(gridWidth, gridHeight, tileSet);
        RunGeneration();
        VisualizeGrid();
    }

    void RunGeneration()
    {
        while (true)
        {
            var cell = grid.GetLowestEntropyCell();
            if (cell == null) break; // Done
            cell.Collapse();
            grid.Propagate(cell);
        }
    }

    void VisualizeGrid()
    {
        // Use the configurable cell size instead of hard-coded value
        float spacing = cellSize;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                var cell = grid.cells[x, y];
                var tile = cell.GetTile();
                if (tile == null) continue;

                Vector3 pos = new Vector3(x * spacing, 0, y * spacing);
                if (tile.prefab != null)
                    Instantiate(tile.prefab, pos, Quaternion.identity);
                else
                {
                    // Debug cube if no prefab
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = pos;
                    cube.GetComponent<Renderer>().material.color = tile.debugColor;
                }
            }
        }
    }
}