using System.Collections.Generic;
using UnityEngine;

public class WFC_Cell
{
    public Vector2Int position;              // Grid position
    public List<WFC_Tile> possibleTiles;     // Tiles this cell can still be
    public bool isCollapsed => possibleTiles.Count == 1;

    public WFC_Cell(Vector2Int pos, List<WFC_Tile> allTiles)
    {
        position = pos;
        possibleTiles = new List<WFC_Tile>(allTiles); // Start with all possibilities
    }

    // Collapse this cell by choosing a tile based on weights
    public void Collapse()
    {
        if (isCollapsed) return;

        float totalWeight = 0;
        foreach (var tile in possibleTiles) totalWeight += tile.weight;

        float rand = Random.Range(0, totalWeight);
        float cumulative = 0;
        foreach (var tile in possibleTiles)
        {
            cumulative += tile.weight;
            if (rand <= cumulative)
            {
                possibleTiles = new List<WFC_Tile> { tile };
                break;
            }
        }
    }

    public WFC_Tile GetTile() => isCollapsed ? possibleTiles[0] : null;
}