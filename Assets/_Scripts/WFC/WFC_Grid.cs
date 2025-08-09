using System.Collections.Generic;
using UnityEngine;

public class WFC_Grid
{
    public int width, height;
    public WFC_Cell[,] cells;
    public List<WFC_Tile> allTiles;

    public WFC_Grid(int width, int height, List<WFC_Tile> tileSet)
    {
        this.width = width;
        this.height = height;
        this.allTiles = tileSet;
        cells = new WFC_Cell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new WFC_Cell(new Vector2Int(x, y), allTiles);
            }
        }
    }

    // Find the cell with the lowest entropy (fewest possible tiles)
    public WFC_Cell GetLowestEntropyCell()
    {
        WFC_Cell lowest = null;
        int minOptions = int.MaxValue;

        foreach (var cell in cells)
        {
            if (!cell.isCollapsed && cell.possibleTiles.Count < minOptions)
            {
                minOptions = cell.possibleTiles.Count;
                lowest = cell;
            }
        }
        return lowest;
    }

    // Propagate constraints from one cell to its neighbors
    public void Propagate(WFC_Cell origin)
    {
        Queue<WFC_Cell> queue = new Queue<WFC_Cell>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            Vector2Int pos = current.position;

            // Check 4 neighbors (up, down, left, right)
            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (var dir in directions)
            {
                Vector2Int neighborPos = pos + dir;
                if (!InBounds(neighborPos)) continue;

                var neighbor = cells[neighborPos.x, neighborPos.y];
                if (neighbor.isCollapsed) continue;

                bool changed = ApplyConstraints(current, neighbor, dir);
                if (changed) queue.Enqueue(neighbor);
            }
        }
    }

    private bool ApplyConstraints(WFC_Cell source, WFC_Cell target, Vector2Int dir)
    {
        // Remove target tiles that aren't compatible with source's collapsed tile
        if (!source.isCollapsed) return false;

        var sourceTile = source.GetTile();
        List<WFC_Tile> allowed = new List<WFC_Tile>();

        // Choose the right neighbor set based on direction
        if (dir == Vector2Int.up) allowed.AddRange(sourceTile.allowedNeighborsUp);
        if (dir == Vector2Int.down) allowed.AddRange(sourceTile.allowedNeighborsDown);
        if (dir == Vector2Int.left) allowed.AddRange(sourceTile.allowedNeighborsLeft);
        if (dir == Vector2Int.right) allowed.AddRange(sourceTile.allowedNeighborsRight);

        int before = target.possibleTiles.Count;
        target.possibleTiles.RemoveAll(tile => !allowed.Contains(tile));

        return target.possibleTiles.Count < before;
    }

    private bool InBounds(Vector2Int pos) =>
        pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
}
