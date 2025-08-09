using System.Collections.Generic;
using UnityEngine;

public class RoomInfo
{
    public GameObject RoomObject { get; set; }
    public RectInt RoomRect { get; set; }
    public bool[,] TileGrid { get; private set; }

    public RoomType Type;


    public bool HasObjects;
    

    public Vector3 Center;
    
    public RoomInfo(GameObject roomObject, RectInt roomRect)
    {
        RoomObject = roomObject;
        RoomRect = roomRect;
        HasObjects = false;
    }
    
    public void SetTileGrid(bool[,] grid)
    {
        TileGrid = grid;
    }

}