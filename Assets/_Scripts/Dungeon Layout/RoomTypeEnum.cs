using UnityEngine;

/// <summary>
/// Enum representing different types of rooms in a dungeon.
/// </summary>
public enum RoomType
{
    /// <summary>
    /// A normal, basic room.
    /// </summary>
    Normal,
    
    /// <summary>
    /// The starting room where the player begins.
    /// </summary>
    Start,
    
    /// <summary>
    /// The final room or exit of the dungeon.
    /// </summary>
    End,
    
    /// <summary>
    /// A room containing treasure or valuable items.
    /// </summary>
    Treasure,
    
    /// <summary>
    /// A room containing a boss enemy.
    /// </summary>
    Boss,
    
    /// <summary>
    /// A room containing traps or hazards.
    /// </summary>
    Trap
}
