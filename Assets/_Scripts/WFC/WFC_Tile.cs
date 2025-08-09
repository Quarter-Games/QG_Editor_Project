using UnityEngine;

[CreateAssetMenu(menuName = "WFC/Tile")]
public class WFC_Tile : ScriptableObject
{
    [Header("Visual Representation")]
    public GameObject prefab;   // Prefab to spawn for this tile
    public Color debugColor = Color.white; // For quick grid visualization

    [Header("Adjacency Rules")]
    public WFC_Tile[] allowedNeighborsUp;
    public WFC_Tile[] allowedNeighborsDown;
    public WFC_Tile[] allowedNeighborsLeft;
    public WFC_Tile[] allowedNeighborsRight;

    [Header("Weight (Probability)")]
    [Range(0.1f, 10f)] public float weight = 1f;
}