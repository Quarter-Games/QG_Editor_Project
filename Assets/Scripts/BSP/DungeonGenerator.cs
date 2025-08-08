using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Dimensions")]
    public int dungeonWidth = 60;
    public int dungeonDepth = 60;
    public int floorCount = 3;
    public float floorHeight = 6f;

    [Header("BSP")]
    [Range(1, 6)] public int maxSplitDepth = 4;

    [Header("Corridors")]
    public int corridorWidth = 2;

    [Header("Prefabs")]
    public BSPFloor floorPrefab;
    public GameObject stairsPrefab;

    List<BSPFloor> floors = new();
    List<NavMeshSurface> surfaces = new();

    void Start() { Build(); }

    public void Build()
    {
        for (int i = 0; i < floorCount; i++)
        {
            Vector3 pos = new(0, i * floorHeight, 0);
            BSPFloor f = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
            f.name = $"Floor_{i}";
            f.Generate(dungeonWidth, dungeonDepth, maxSplitDepth, corridorWidth);
            floors.Add(f);
            surfaces.Add(f.GetComponent<NavMeshSurface>());
        }

        for (int i = 0; i < floorCount - 1; i++)
            PlaceStairsBetween(i, i + 1);

        foreach (var s in surfaces) s.BuildNavMesh();
    }

    void PlaceStairsBetween(int lowerIdx, int upperIdx)
    {
        BSPFloor low = floors[lowerIdx];
        BSPFloor high = floors[upperIdx];

        RectInt room = low.Rooms[Random.Range(0, low.Rooms.Count)];
        Vector3 local = new(room.center.x - dungeonWidth / 2,
                            0,
                            room.center.y - dungeonDepth / 2);

        Vector3 worldPos = low.transform.TransformPoint(local + Vector3.up * 0.5f);
        GameObject stairs = Instantiate(stairsPrefab, worldPos, Quaternion.identity, low.transform);

        var link = stairs.GetComponent<NavMeshLink>();
        link.startPoint = Vector3.zero;
        link.endPoint = Vector3.up * floorHeight;
        link.UpdateLink();
    }
}
