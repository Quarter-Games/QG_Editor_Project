using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

[ExecuteAlways]
public class DungeonGenerator : MonoBehaviour
{
    [Header("Seed & Scale")] public int seed = 12345;
    public float cellSize = 1f;

    [Header("Dungeon Size")] public int dungeonWidth = 64;
    public int dungeonDepth = 64;
    public int floors = 2;
    public float floorHeight = 6f;

    [Header("BSP")] public int maxSplitDepth = 4;
    public int minLeafSize = 12;
    public int corridorWidth = 2;

    [Header("Rooms & Quest")] public List<RoomType> roomTypes;
    public int numLockedRooms = 1;
    public int numTreasureRooms = 1;

    [Header("Prefabs")] public GameObject floorTilePrefab;
    public GameObject wallSegmentPrefab;
    public GameObject closedDoorPrefab;
    public GameObject keyPickupPrefab;
    public GameObject stairsUpPrefab;
    public GameObject stairsDownPrefab;

    [Header("NavMesh")] public NavMeshSurface navSurface;

    [Header("Runtime")] public bool autoGenerateOnStart = false;
    public bool clearBeforeGenerate = true;

    private System.Random rng;
    private List<Room> allRooms = new();
    private List<Corridor> allCorridors = new();
    private List<(Vector3 from, Vector3 to)> stairLinks = new();
    private Dictionary<Room, GameObject> roomRoots = new();

    private struct RectI { public int x, z, w, d; public RectI(int x, int z, int w, int d) { this.x = x; this.z = z; this.w = w; this.d = d; } }
    private class BSPNode { public RectI area; public BSPNode left, right; public RectI? roomRect; public int floorIdx; public BSPNode(RectI r, int floor) { area = r; floorIdx = floor; } }
    private class Room { public RectI rect; public int floorIdx; public RoomType type; public string keyId; public Vector3 Center(float yBase, float hPerFloor) => new Vector3((rect.x + rect.w / 2f), yBase + floorIdx * hPerFloor, (rect.z + rect.d / 2f)); }
    private struct Corridor { public Vector3 a, b; public int floor; }

    void Start() { if (Application.isPlaying && autoGenerateOnStart) Generate(); }

    [ContextMenu("Generate Dungeon")]
    public void Generate()
    {
        if (!ValidatePrefabs()) { Debug.LogWarning("Assign all required prefabs & NavMeshSurface."); return; }
        rng = new System.Random(seed);
        if (clearBeforeGenerate) ClearChildren();
        allRooms.Clear(); allCorridors.Clear(); stairLinks.Clear(); roomRoots.Clear();

        var floorsNodes = new List<BSPNode>();
        for (int f = 0; f < floors; f++)
        {
            RectI full = new RectI(0, 0, dungeonWidth, dungeonDepth);
            var root = new BSPNode(full, f);
            SplitRecursive(root, 0);
            CreateRoomsFromLeaves(root);
            floorsNodes.Add(root);
        }

        foreach (var root in floorsNodes) ConnectFloor(root);
        for (int f = 0; f < floors - 1; f++) LinkFloorsWithStairs(f, f + 1);

        BuildGeometry();
        if (navSurface) navSurface.BuildNavMesh();
    }

    private bool ValidatePrefabs() => floorTilePrefab && wallSegmentPrefab && closedDoorPrefab && keyPickupPrefab && stairsUpPrefab && stairsDownPrefab && navSurface;

    private void ClearChildren()
    {
        var toDestroy = new List<GameObject>();
        foreach (Transform c in transform) toDestroy.Add(c.gameObject);
        foreach (var go in toDestroy) { if (!Application.isPlaying) DestroyImmediate(go); else Destroy(go); }
    }

    private void SplitRecursive(BSPNode n, int depth)
    {
        if (depth >= maxSplitDepth || n.area.w < minLeafSize * 2 || n.area.d < minLeafSize * 2) return;
        bool splitVert = rng.NextDouble() < 0.5;
        if (n.area.w < n.area.d) splitVert = false;
        if (n.area.d < n.area.w) splitVert = true;

        if (splitVert)
        {
            int splitX = rng.Next(n.area.x + minLeafSize, n.area.x + n.area.w - minLeafSize);
            n.left = new BSPNode(new RectI(n.area.x, n.area.z, splitX - n.area.x, n.area.d), n.floorIdx);
            n.right = new BSPNode(new RectI(splitX, n.area.z, n.area.x + n.area.w - splitX, n.area.d), n.floorIdx);
        }
        else
        {
            int splitZ = rng.Next(n.area.z + minLeafSize, n.area.z + n.area.d - minLeafSize);
            n.left = new BSPNode(new RectI(n.area.x, n.area.z, n.area.w, splitZ - n.area.z), n.floorIdx);
            n.right = new BSPNode(new RectI(n.area.x, splitZ, n.area.w, n.area.z + n.area.d - splitZ), n.floorIdx);
        }

        SplitRecursive(n.left, depth + 1);
        SplitRecursive(n.right, depth + 1);
    }

    private void CreateRoomsFromLeaves(BSPNode n)
    {
        if (n.left == null && n.right == null)
        {
            int pad = 2;
            int w = rng.Next(minLeafSize / 2, n.area.w - pad);
            int d = rng.Next(minLeafSize / 2, n.area.d - pad);
            int x = rng.Next(n.area.x + 1, n.area.x + n.area.w - w - 1);
            int z = rng.Next(n.area.z + 1, n.area.z + n.area.d - d - 1);
            n.roomRect = new RectI(x, z, w, d);

            var rt = PickRoomTypeForFloor(n.floorIdx);
            var room = new Room { rect = n.roomRect.Value, floorIdx = n.floorIdx, type = rt, keyId = rt ? rt.keyId : null };
            allRooms.Add(room);
            return;
        }
        if (n.left != null) CreateRoomsFromLeaves(n.left);
        if (n.right != null) CreateRoomsFromLeaves(n.right);
    }

    private RoomType PickRoomTypeForFloor(int floor)
    {
        if (roomTypes == null || roomTypes.Count == 0) return null;
        for (int i = 0; i < 10; i++)
        {
            var tryRt = roomTypes[rng.Next(roomTypes.Count)];
            if (floor >= tryRt.minFloor && floor <= tryRt.maxFloor) return tryRt;
        }
        return roomTypes[0];
    }

    private void ConnectFloor(BSPNode root)
    {
        var floorRooms = allRooms.FindAll(r => r.floorIdx == root.floorIdx);
        for (int i = 0; i < floorRooms.Count; i++)
        {
            var a = floorRooms[i];
            float best = float.MaxValue; Room bestB = null;
            for (int j = 0; j < floorRooms.Count; j++)
            {
                if (i == j) continue;
                var b = floorRooms[j];
                float d = Vector3.SqrMagnitude(a.Center(transform.position.y, floorHeight) - b.Center(transform.position.y, floorHeight));
                if (d < best) { best = d; bestB = b; }
            }
            if (bestB != null) allCorridors.Add(new Corridor { a = a.Center(transform.position.y, floorHeight), b = bestB.Center(transform.position.y, floorHeight), floor = a.floorIdx });
        }
    }

    private void LinkFloorsWithStairs(int fA, int fB)
    {
        var roomsA = allRooms.FindAll(r => r.floorIdx == fA);
        var roomsB = allRooms.FindAll(r => r.floorIdx == fB);
        if (roomsA.Count == 0 || roomsB.Count == 0) return;
        var ra = roomsA[rng.Next(roomsA.Count)];
        Vector3 ca = ra.Center(transform.position.y, floorHeight);
        float best = float.MaxValue; Room bestB = null;
        foreach (var rb in roomsB)
        {
            float d = Vector3.SqrMagnitude(new Vector3(rb.Center(0, 0).x, 0, rb.Center(0, 0).z) - new Vector3(ca.x, 0, ca.z));
            if (d < best) { best = d; bestB = rb; }
        }
        if (bestB != null) stairLinks.Add((ca, bestB.Center(transform.position.y, floorHeight)));
    }

    private void BuildGeometry()
    {
        var floorRoots = new List<Transform>();
        for (int f = 0; f < floors; f++)
        {
            var root = new GameObject($"Floor_{f}").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0, transform.position.y + f * floorHeight, 0);
            floorRoots.Add(root);
        }

        foreach (var r in allRooms)
        {
            Transform parent = floorRoots[r.floorIdx];
            var roomGo = new GameObject($"{r.type?.kind.ToString() ?? "Room"}_{r.floorIdx}").transform;
            roomGo.SetParent(parent, false);
            roomRoots[r] = roomGo.gameObject;

            for (int x = 0; x < r.rect.w; x++)
                for (int z = 0; z < r.rect.d; z++)
                    Instantiate(floorTilePrefab, parent).transform.localPosition = new Vector3(r.rect.x + x + 0.5f, 0, r.rect.z + z + 0.5f);

            for (int x = 0; x < r.rect.w; x++)
            {
                PlaceWall(parent, new Vector3(r.rect.x + x + 0.5f, 0, r.rect.z - 0.5f), true);
                PlaceWall(parent, new Vector3(r.rect.x + x + 0.5f, 0, r.rect.z + r.rect.d + 0.5f), true);
            }
            for (int z = 0; z < r.rect.d; z++)
            {
                PlaceWall(parent, new Vector3(r.rect.x - 0.5f, 0, r.rect.z + z + 0.5f), false);
                PlaceWall(parent, new Vector3(r.rect.x + r.rect.w + 0.5f, 0, r.rect.z + z + 0.5f), false);
            }

            if (r.type && r.type.decoration) DecorateRoom(r, parent);
        }

        foreach (var c in allCorridors) DrawCorridor(floorRoots[c.floor], c.a, c.b);

        foreach (var r in allRooms)
        {
            if (r.type == null) continue;
            if (r.type.kind == RoomKind.Locked)
            {
                var d = Instantiate(closedDoorPrefab, roomRoots[r].transform.parent);
                d.transform.localPosition = new Vector3(r.rect.x + r.rect.w / 2f, 0, r.rect.z - 0.5f);
                var door = d.GetComponent<Door>(); if (door) door.keyId = r.keyId;
            }
            if (r.type.kind == RoomKind.Key)
            {
                var k = Instantiate(keyPickupPrefab, roomRoots[r].transform.parent);
                k.transform.localPosition = new Vector3(r.rect.x + r.rect.w / 2f, 0.6f, r.rect.z + r.rect.d / 2f);
                var item = k.GetComponent<ItemPickup>(); if (item) item.keyId = r.keyId;
            }
        }

        foreach (var link in stairLinks)
        {
            Instantiate(stairsUpPrefab, link.from, Quaternion.identity, transform);
            Instantiate(stairsDownPrefab, link.to, Quaternion.identity, transform);
        }
    }

    private void PlaceWall(Transform parent, Vector3 localPos, bool alongX)
    {
        var w = Instantiate(wallSegmentPrefab, parent);
        w.transform.localRotation = alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
        w.transform.localPosition = localPos + Vector3.up * (wallSegmentPrefab.transform.localScale.y * 0.5f);
    }

    private void DrawCorridor(Transform parent, Vector3 a, Vector3 b)
    {
        int x0 = Mathf.RoundToInt(a.x), z0 = Mathf.RoundToInt(a.z);
        int x1 = Mathf.RoundToInt(b.x), z1 = Mathf.RoundToInt(b.z);
        int dx = x1 >= x0 ? 1 : -1;
        for (int x = x0; x != x1; x += dx)
            for (int w = -corridorWidth / 2; w <= corridorWidth / 2; w++)
                Instantiate(floorTilePrefab, parent).transform.localPosition = new Vector3(x + 0.5f, 0, z0 + w + 0.5f);
        int dz = z1 >= z0 ? 1 : -1;
        for (int z = z0; z != z1; z += dz)
            for (int w = -corridorWidth / 2; w <= corridorWidth / 2; w++)
                Instantiate(floorTilePrefab, parent).transform.localPosition = new Vector3(x1 + w + 0.5f, 0, z + 0.5f);
    }

    private void DecorateRoom(Room r, Transform parent)
    {
        var prof = r.type.decoration;
        float step = Mathf.Max(0.5f, prof.sampleStep);
        for (float x = r.rect.x + 1; x < r.rect.x + r.rect.w - 1; x += step)
            for (float z = r.rect.z + 1; z < r.rect.z + r.rect.d - 1; z += step)
            {
                if (Random.value <= 0.3f)
                    Instantiate(prof.rules[Random.Range(0, prof.rules.Length)].prefab, parent).transform.localPosition = new Vector3(x + 0.5f, 0, z + 0.5f);
            }
    }

    private void OnDrawGizmosSelected()
    {
        foreach (var r in allRooms)
        {
            Gizmos.color = r.type ? r.type.gizmoColor : Color.gray;
            Gizmos.DrawWireCube(r.Center(transform.position.y, floorHeight), new Vector3(r.rect.w, 0.1f, r.rect.d));
        }
        Gizmos.color = Color.white;
        foreach (var c in allCorridors) Gizmos.DrawLine(c.a, c.b);
        Gizmos.color = Color.cyan;
        foreach (var s in stairLinks) Gizmos.DrawLine(s.from, s.to);
    }
}
