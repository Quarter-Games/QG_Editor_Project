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
    public GameObject ladderPrefab;

    [Header("NavMesh")] public NavMeshSurface navSurface;

    [Header("Ladders")]
    [Tooltip("Yaw rotation (deg) to face the ladder against the wall (0 = +Z).")]
    public float ladderYaw = 0f;
    [Tooltip("Push the ladder slightly off the wall to avoid z-fighting.")]
    public float ladderClearance = 0.05f;
    [Tooltip("Cut a small opening on the upper floor where the ladder ends.")]
    public bool cutTopOpening = true;
    [Tooltip("Opening size in tiles (x,z) around the ladder top.")]
    public Vector2Int ladderOpeningSize = new Vector2Int(1, 1);

    [Header("Runtime")] public bool autoGenerateOnStart = false;
    public bool clearBeforeGenerate = true;

    private System.Random rng;

    // Data collections
    private List<Room> allRooms = new List<Room>();
    private List<Corridor> allCorridors = new List<Corridor>();
    private List<LadderLink> ladderLinks = new List<LadderLink>();
    private Dictionary<Room, GameObject> roomRoots = new Dictionary<Room, GameObject>();

    private HashSet<Vector3Int> corridorCells = new HashSet<Vector3Int>();

    // Track placed wall segments for carving/doors
    private struct WallKey { public int x, z; public bool alongX; public int floor; public WallKey(int x, int z, bool alongX, int floor) { this.x = x; this.z = z; this.alongX = alongX; this.floor = floor; } }
    private Dictionary<WallKey, GameObject> wallAt = new Dictionary<WallKey, GameObject>();

    // Floor tile registry so we can punch stair holes precisely (floor, x, z)
    private struct TileKey { public int floor, x, z; public TileKey(int f, int x, int z) { floor = f; this.x = x; this.z = z; } }
    private Dictionary<TileKey, GameObject> floorAt = new Dictionary<TileKey, GameObject>();

    private struct RectI { public int x, z, w, d; public RectI(int x, int z, int w, int d) { this.x = x; this.z = z; this.w = w; this.d = d; } }
    private class BSPNode { public RectI area; public BSPNode left, right; public RectI? roomRect; public int floorIdx; public BSPNode(RectI r, int floor) { area = r; floorIdx = floor; } }
    private class Room { public RectI rect; public int floorIdx; public RoomType type; public string keyId; public Vector3 Center(float yBase, float hPerFloor) { return new Vector3((rect.x + rect.w / 2f), yBase + floorIdx * hPerFloor, (rect.z + rect.d / 2f)); } }
    private struct Corridor { public Vector3 a, b; public int floor; }
    private struct LadderLink { public Vector3 from; public Vector3 to; public LadderLink(Vector3 f, Vector3 t) { from = f; to = t; } }

    void Start()
    {
        if (Application.isPlaying && autoGenerateOnStart) Generate();
    }

    [ContextMenu("Generate Dungeon")]
    public void Generate()
    {
        if (!ValidatePrefabs()) { Debug.LogWarning("Assign all required prefabs & NavMeshSurface."); return; }
        rng = new System.Random(seed);

        if (clearBeforeGenerate) ClearChildren();
        allRooms.Clear(); allCorridors.Clear(); ladderLinks.Clear(); roomRoots.Clear();
        corridorCells.Clear(); wallAt.Clear(); floorAt.Clear();

        var floorsNodes = new List<BSPNode>();
        for (int f = 0; f < floors; f++)
        {
            RectI full = new RectI(0, 0, dungeonWidth, dungeonDepth);
            var root = new BSPNode(full, f);
            SplitRecursive(root, 0);
            CreateRoomsFromLeaves(root);
            floorsNodes.Add(root);
        }

        foreach (var root in floorsNodes) ConnectFloorEnsuringReachability(root);
        for (int f = 0; f < floors - 1; f++) LinkFloorsWithLadders(f, f + 1);

        // Precompute corridor occupancy
        PopulateCorridorCells();

        BuildGeometry();
        if (navSurface) navSurface.BuildNavMesh();
    }

    private bool ValidatePrefabs()
    {
        return floorTilePrefab && wallSegmentPrefab && closedDoorPrefab && keyPickupPrefab && ladderPrefab && navSurface;
    }

    private void ClearChildren()
    {
        var toDestroy = new List<GameObject>();
        foreach (Transform c in transform) toDestroy.Add(c.gameObject);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            foreach (var go in toDestroy) DestroyImmediate(go);
        }
        else
        {
            foreach (var go in toDestroy) Destroy(go);
        }
#else
        foreach (var go in toDestroy) Destroy(go);
#endif
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
            var room = new Room { rect = n.roomRect.Value, floorIdx = n.floorIdx, type = rt, keyId = (rt != null ? rt.keyId : null) };
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

    // Simpler connectivity: connect nearest unconnected room repeatedly
    private void ConnectFloorEnsuringReachability(BSPNode root)
    {
        var floorRooms = allRooms.FindAll(r => r.floorIdx == root.floorIdx);
        if (floorRooms.Count <= 1) return;

        var connected = new List<Room> { floorRooms[0] };
        var remaining = new List<Room>();
        for (int i = 1; i < floorRooms.Count; i++) remaining.Add(floorRooms[i]);

        while (remaining.Count > 0)
        {
            float best = float.MaxValue; Room from = null; Room to = null;
            foreach (var a in connected)
            {
                foreach (var b in remaining)
                {
                    float d = Vector3.SqrMagnitude(a.Center(transform.position.y, floorHeight) - b.Center(transform.position.y, floorHeight));
                    if (d < best) { best = d; from = a; to = b; }
                }
            }
            if (from == null || to == null) break;
            allCorridors.Add(new Corridor { a = from.Center(transform.position.y, floorHeight), b = to.Center(transform.position.y, floorHeight), floor = from.floorIdx });
            connected.Add(to);
            remaining.Remove(to);
        }
    }

    private void LinkFloorsWithLadders(int fA, int fB)
    {
        if (fA < 0 || fB <= fA) return;
        var roomsA = allRooms.FindAll(r => r.floorIdx == fA);
        var roomsB = allRooms.FindAll(r => r.floorIdx == fB);
        if (roomsA.Count == 0 || roomsB.Count == 0) return;

        Room raBest = null, rbBest = null; float best = float.MaxValue;
        foreach (var ra in roomsA)
        {
            foreach (var rb in roomsB)
            {
                float d = Vector2.SqrMagnitude(new Vector2(ra.Center(0, 0).x, ra.Center(0, 0).z) - new Vector2(rb.Center(0, 0).x, rb.Center(0, 0).z));
                if (d < best) { best = d; raBest = ra; rbBest = rb; }
            }
        }
        if (raBest == null || rbBest == null) return;

        int ax0 = raBest.rect.x + 1, ax1 = raBest.rect.x + raBest.rect.w - 2;
        int az0 = raBest.rect.z + 1, az1 = raBest.rect.z + raBest.rect.d - 2;
        int bx0 = rbBest.rect.x + 1, bx1 = rbBest.rect.x + rbBest.rect.w - 2;
        int bz0 = rbBest.rect.z + 1, bz1 = rbBest.rect.z + rbBest.rect.d - 2;
        int sx = Mathf.Clamp(Mathf.RoundToInt((raBest.rect.x + raBest.rect.w * 0.5f + rbBest.rect.x + rbBest.rect.w * 0.5f) * 0.5f), Mathf.Max(ax0, bx0), Mathf.Min(ax1, bx1));
        int sz = Mathf.Clamp(Mathf.RoundToInt((raBest.rect.z + raBest.rect.d * 0.5f + rbBest.rect.z + rbBest.rect.d * 0.5f) * 0.5f), Mathf.Max(az0, bz0), Mathf.Min(az1, bz1));

        Vector3 from = new Vector3(sx + 0.5f, transform.position.y + fA * floorHeight, sz + 0.5f);
        Vector3 to = new Vector3(sx + 0.5f, transform.position.y + fB * floorHeight, sz + 0.5f);
        ladderLinks.Add(new LadderLink(from, to));
    }

    private void BuildGeometry()
    {
        var floorRoots = new List<Transform>();
        for (int f = 0; f < floors; f++)
        {
            var root = new GameObject($"Floor_{f}").transform;
            root.SetParent(transform, false);
            // Position floors relative to this generator (local Y), not world Y
            root.localPosition = new Vector3(0, f * floorHeight, 0);
            floorRoots.Add(root);
        }

        foreach (var r in allRooms)
        {
            Transform parent = floorRoots[r.floorIdx];
            var roomGo = new GameObject($"{(r.type != null ? r.type.kind.ToString() : "Room")}_{r.floorIdx}").transform;
            roomGo.SetParent(parent, false);
            roomRoots[r] = roomGo.gameObject;

            for (int x = 0; x < r.rect.w; x++)
            {
                for (int z = 0; z < r.rect.d; z++)
                {
                    var ft = Instantiate(floorTilePrefab, parent);
                    ft.transform.localPosition = new Vector3(r.rect.x + x + 0.5f, 0, r.rect.z + z + 0.5f);
                    floorAt[new TileKey(r.floorIdx, r.rect.x + x, r.rect.z + z)] = ft;
                }
            }

            float tX = GetWallHalfThickness(false);
            float tZ = GetWallHalfThickness(true);

            for (int x = 0; x < r.rect.w; x++)
            {
                PlaceWall(parent, new Vector3(r.rect.x + x + 0.5f, 0, r.rect.z - tZ), true, r.rect.x + x, r.rect.z);
                PlaceWall(parent, new Vector3(r.rect.x + x + 0.5f, 0, r.rect.z + r.rect.d + tZ), true, r.rect.x + x, r.rect.z + r.rect.d + 1);
            }
            for (int z = 0; z < r.rect.d; z++)
            {
                PlaceWall(parent, new Vector3(r.rect.x - tX, 0, r.rect.z + z + 0.5f), false, r.rect.x, r.rect.z + z);
                PlaceWall(parent, new Vector3(r.rect.x + r.rect.w + tX, 0, r.rect.z + z + 0.5f), false, r.rect.x + r.rect.w + 1, r.rect.z + z);
            }

            if (r.type != null && r.type.decoration != null) DecorateRoom(r, parent);
        }

        foreach (var c in allCorridors) DrawCorridor(floorRoots[c.floor], c.a, c.b);
        // NEW: add corridor side walls (will not block room openings)
        BuildCorridorWalls(floorRoots);
        CarveEntrancesAndDoors(floorRoots);

        foreach (var r in allRooms)
        {
            if (r.type == null) continue;
            if (r.type.kind == RoomKind.Key)
            {
                var kgo = Instantiate(keyPickupPrefab, roomRoots[r].transform.parent);
                kgo.transform.localPosition = new Vector3(r.rect.x + r.rect.w / 2f, 0.6f, r.rect.z + r.rect.d / 2f);
                var item = kgo.GetComponent<ItemPickup>(); if (item) item.keyId = r.keyId;
            }
        }

        foreach (var link in ladderLinks) PlaceLadderLink(link.from, link.to);
    }

    private void PopulateCorridorCells()
    {
        corridorCells.Clear();
        for (int i = 0; i < allCorridors.Count; i++)
        {
            var c = allCorridors[i];
            int floor = c.floor;
            int x0 = Mathf.RoundToInt(c.a.x), z0 = Mathf.RoundToInt(c.a.z);
            int x1 = Mathf.RoundToInt(c.b.x), z1 = Mathf.RoundToInt(c.b.z);

            int dx = x1 >= x0 ? 1 : -1;
            for (int x = x0; x != x1; x += dx)
                for (int w = -corridorWidth / 2; w <= corridorWidth / 2; w++)
                    corridorCells.Add(new Vector3Int(x, floor, z0 + w));

            int dz = z1 >= z0 ? 1 : -1;
            for (int z = z0; z != z1; z += dz)
                for (int w = -corridorWidth / 2; w <= corridorWidth / 2; w++)
                    corridorCells.Add(new Vector3Int(x1 + w, floor, z));

            for (int w = -corridorWidth / 2; w <= corridorWidth / 2; w++)
            {
                corridorCells.Add(new Vector3Int(x0, floor, z0 + w));
                corridorCells.Add(new Vector3Int(x1 + w, floor, z1));
            }
        }
    }

    private bool IsCorridorTile(int x, int z, int floor)
    {
        return corridorCells.Contains(new Vector3Int(x, floor, z));
    }

    private void DrawCorridor(Transform parent, Vector3 a, Vector3 b)
    {
        int floor = GetFloorIndexFromParent(parent);
        int x0 = Mathf.RoundToInt(a.x), z0 = Mathf.RoundToInt(a.z);
        int x1 = Mathf.RoundToInt(b.x), z1 = Mathf.RoundToInt(b.z);

        int dx = x1 >= x0 ? 1 : -1;
        for (int x = x0; x != x1; x += dx)
        {
            for (int w = -corridorWidth / 2; w <= corridorWidth / 2; w++)
            {
                var t = Instantiate(floorTilePrefab, parent);
                t.transform.localPosition = new Vector3(x + 0.5f, 0, z0 + w + 0.5f);
                floorAt[new TileKey(floor, x, z0 + w)] = t;
            }
        }
        int dz = z1 >= z0 ? 1 : -1;
        for (int z = z0; z != z1; z += dz)
        {
            for (int w = -corridorWidth / 2; w <= corridorWidth / 2; w++)
            {
                var t = Instantiate(floorTilePrefab, parent);
                t.transform.localPosition = new Vector3(x1 + w + 0.5f, 0, z + 0.5f);
                floorAt[new TileKey(floor, x1 + w, z)] = t;
            }
        }
        for (int w = -corridorWidth / 2; w <= corridorWidth / 2; w++)
        {
            var t = Instantiate(floorTilePrefab, parent);
            t.transform.localPosition = new Vector3(x1 + w + 0.5f, 0, z1 + 0.5f);
            floorAt[new TileKey(floor, x1 + w, z1)] = t;
        }
    }

    private void CarveEntrancesAndDoors(List<Transform> floorRoots)
    {
        for (int i = 0; i < allRooms.Count; i++)
        {
            var r = allRooms[i];
            var parent = floorRoots[r.floorIdx];
            var rect = r.rect;

            float tX = GetWallHalfThickness(false);
            float tZ = GetWallHalfThickness(true);

            var doorPositions = new List<(Vector3 pos, bool alongX)>();

            CarveEdgeX(rect, r.floorIdx, rect.z - 1, rect.z - tZ, rect.z, parent, doorPositions);
            CarveEdgeX(rect, r.floorIdx, rect.z + rect.d, rect.z + rect.d + tZ, rect.z + rect.d + 1, parent, doorPositions);
            CarveEdgeZ(rect, r.floorIdx, rect.x - 1, rect.x - tX, rect.x, parent, doorPositions);
            CarveEdgeZ(rect, r.floorIdx, rect.x + rect.w, rect.x + rect.w + tX, rect.x + rect.w + 1, parent, doorPositions);

            if (r.type != null && r.type.kind == RoomKind.Locked)
            {
                for (int d = 0; d < doorPositions.Count; d++)
                {
                    var dp = doorPositions[d];
                    PlaceDoorAt(r, dp.pos, dp.alongX, parent);
                }
            }
        }
    }

    private void CarveEdgeX(RectI rect, int floor, int zEdgeCell, float zPlane, int keyZ, Transform parent, List<(Vector3 pos, bool alongX)> doorOut)
    {
        var xs = new List<int>();
        for (int x = rect.x; x < rect.x + rect.w; x++) if (IsCorridorTile(x, zEdgeCell, floor)) xs.Add(x);
        if (xs.Count == 0) return;
        int min = xs[0], max = xs[xs.Count - 1];
        for (int x = min; x <= max; x++) RemoveWallKey(new WallKey(x, keyZ, true, floor));

        var room = GetRoomAt(rect, floor);
        if (room != null && room.type != null && room.type.kind == RoomKind.Locked)
        {
            int width = max - min + 1;
            int centerX = (min + max) / 2;
            if (width > 1)
            {
                for (int x = min; x <= max; x++)
                {
                    if (x == centerX) continue;
                    PlaceWall(parent, new Vector3(x + 0.5f, 0, zPlane), true, x, keyZ);
                }
            }
            doorOut.Add((new Vector3(centerX + 0.5f, 0f, zPlane), true));
        }
    }

    private void CarveEdgeZ(RectI rect, int floor, int xEdgeCell, float xPlane, int keyX, Transform parent, List<(Vector3 pos, bool alongX)> doorOut)
    {
        var zs = new List<int>();
        for (int z = rect.z; z < rect.z + rect.d; z++) if (IsCorridorTile(xEdgeCell, z, floor)) zs.Add(z);
        if (zs.Count == 0) return;
        int min = zs[0], max = zs[zs.Count - 1];
        for (int z = min; z <= max; z++) RemoveWallKey(new WallKey(keyX, z, false, floor));

        var room = GetRoomAt(rect, floor);
        if (room != null && room.type != null && room.type.kind == RoomKind.Locked)
        {
            int width = max - min + 1;
            int centerZ = (min + max) / 2;
            if (width > 1)
            {
                for (int z = min; z <= max; z++)
                {
                    if (z == centerZ) continue;
                    PlaceWall(parent, new Vector3(xPlane, 0, z + 0.5f), false, keyX, z);
                }
            }
            doorOut.Add((new Vector3(xPlane, 0f, centerZ + 0.5f), false));
        }
    }

    private Room GetRoomAt(RectI rect, int floor)
    {
        for (int i = 0; i < allRooms.Count; i++)
        {
            var rr = allRooms[i];
            if (rr.floorIdx != floor) continue;
            if (rr.rect.x == rect.x && rr.rect.z == rect.z && rr.rect.w == rect.w && rr.rect.d == rect.d) return rr;
        }
        return null;
    }

    private void RemoveWallKey(WallKey key)
    {
        GameObject wgo;
        if (wallAt.TryGetValue(key, out wgo) && wgo)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(wgo);
            else Destroy(wgo);
#else
            Destroy(wgo);
#endif
        }
        wallAt.Remove(key);
    }

    private void PlaceDoorAt(Room r, Vector3 localPos, bool alongX, Transform floorParent)
    {
        var d = Instantiate(closedDoorPrefab, floorParent);
        d.transform.localRotation = alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
        d.transform.localPosition = localPos;
        AlignBottomToFloor(d, 0f);
        var door = d.GetComponent<Door>(); if (door) door.keyId = r.keyId;
    }

    private void AlignBottomToFloor(GameObject go, float localFloorY)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
        {
            var cols = go.GetComponentsInChildren<Collider>();
            if (cols.Length == 0) return;
            Bounds b = TransformToLocalBounds(cols[0].bounds, go.transform.parent);
            for (int i = 1; i < cols.Length; i++) b.Encapsulate(TransformToLocalBounds(cols[i].bounds, go.transform.parent));
            float dy = localFloorY - b.min.y;
            go.transform.localPosition += new Vector3(0f, dy, 0f);
            return;
        }
        else
        {
            Bounds b = TransformToLocalBounds(rends[0].bounds, go.transform.parent);
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(TransformToLocalBounds(rends[i].bounds, go.transform.parent));
            float dy = localFloorY - b.min.y;
            go.transform.localPosition += new Vector3(0f, dy, 0f);
        }
    }

    private static Bounds TransformToLocalBounds(Bounds worldBounds, Transform localSpace)
    {
        var center = localSpace.InverseTransformPoint(worldBounds.center);
        var extents = worldBounds.extents;
        return new Bounds(center, extents * 2f);
    }

    private void DecorateRoom(Room r, Transform parent)
    {
        if (r.type == null || r.type.decoration == null || r.type.decoration.rules == null || r.type.decoration.rules.Length == 0) return;
        var prof = r.type.decoration;
        float step = Mathf.Max(0.5f, prof.sampleStep);
        for (float x = r.rect.x + 1; x < r.rect.x + r.rect.w - 1; x += step)
        {
            for (float z = r.rect.z + 1; z < r.rect.z + r.rect.d - 1; z += step)
            {
                var rule = prof.rules[Random.Range(0, prof.rules.Length)];
                if (rule.prefab == null) continue;
                if (Random.value <= Mathf.Clamp01(rule.probability))
                {
                    Instantiate(rule.prefab, parent).transform.localPosition = new Vector3(x + 0.5f, 0, z + 0.5f);
                }
            }
        }
    }

    private int GetFloorIndexFromParent(Transform parent)
    {
        var t = parent;
        while (t != null && !t.name.StartsWith("Floor_")) t = t.parent;
        if (t != null && t.name.StartsWith("Floor_"))
        {
            string s = t.name.Substring(6);
            int f; if (int.TryParse(s, out f)) return f;
        }
        return 0;
    }

    private int WorldToFloorIndex(Vector3 world)
    {
        return Mathf.RoundToInt((world.y - transform.position.y) / floorHeight);
    }

    private float GetPrefabHalfHeight(GameObject prefab)
    {
        if (!prefab) return 0.5f;
        var col = prefab.GetComponentInChildren<Collider>();
        if (col) return col.bounds.size.y * 0.5f;
        var rend = prefab.GetComponentInChildren<Renderer>();
        if (rend) return rend.bounds.size.y * 0.5f;
        return Mathf.Max(0.5f, prefab.transform.localScale.y * 0.5f);
    }

    private void RemoveFloorTileAtWorld(Vector3 world)
    {
        RemoveFloorPatchAtWorld(world, 0);
    }

    private void RemoveFloorPatchAtWorld(Vector3 world, int halfRadius)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        int floor = WorldToFloorIndex(world);
        int cx = Mathf.FloorToInt(local.x);
        int cz = Mathf.FloorToInt(local.z);
        for (int dx = -halfRadius; dx <= halfRadius; dx++)
        {
            for (int dz = -halfRadius; dz <= halfRadius; dz++)
            {
                var key = new TileKey(floor, cx + dx, cz + dz);
                GameObject tile;
                if (floorAt.TryGetValue(key, out tile) && tile)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(tile);
                    else Destroy(tile);
#else
                    Destroy(tile);
#endif
                    floorAt.Remove(key);
                }
            }
        }
    }

    private float GetWallHalfThickness(bool alongX)
    {
        float t = 0.1f;
        if (wallSegmentPrefab)
        {
            var rend = wallSegmentPrefab.GetComponentInChildren<Renderer>();
            if (rend) { var s = rend.bounds.size; t = alongX ? s.z : s.x; }
            else { var ls = wallSegmentPrefab.transform.localScale; t = alongX ? ls.z : ls.x; }
        }
        return Mathf.Max(0.01f, t * 0.5f);
    }

    // Corridor walls -------------------------------------------------------
    private void BuildCorridorWalls(List<Transform> floorRoots)
    {
        float tX = GetWallHalfThickness(false); // thickness offset along X for vertical walls
        float tZ = GetWallHalfThickness(true);  // thickness offset along Z for horizontal walls

        foreach (var cell in corridorCells)
        {
            int x = cell.x; int z = cell.z; int f = cell.y; // stored as (x,floor,z)
            var parent = floorRoots[f];

            // +Z edge (north)
            if (!IsCorridorTile(x, z + 1, f) && !IsRoomCell(f, x, z + 1))
            {
                PlaceCorridorWall(parent, new Vector3(x + 0.5f, 0, z + 1f + tZ), true);
            }
            // -Z edge (south)
            if (!IsCorridorTile(x, z - 1, f) && !IsRoomCell(f, x, z - 1))
            {
                PlaceCorridorWall(parent, new Vector3(x + 0.5f, 0, z - tZ), true);
            }
            // +X edge (east)
            if (!IsCorridorTile(x + 1, z, f) && !IsRoomCell(f, x + 1, z))
            {
                PlaceCorridorWall(parent, new Vector3(x + 1f + tX, 0, z + 0.5f), false);
            }
            // -X edge (west)
            if (!IsCorridorTile(x - 1, z, f) && !IsRoomCell(f, x - 1, z))
            {
                PlaceCorridorWall(parent, new Vector3(x - tX, 0, z + 0.5f), false);
            }
        }
    }

    private void PlaceCorridorWall(Transform parent, Vector3 localPos, bool alongX)
    {
        var w = Instantiate(wallSegmentPrefab, parent);
        w.transform.localRotation = alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
        float halfH = GetPrefabHalfHeight(wallSegmentPrefab);
        w.transform.localPosition = localPos + Vector3.up * halfH;
        // Intentionally NOT registering in wallAt so room carving won’t remove corridor walls
    }

    private bool IsRoomCell(int floor, int x, int z)
    {
        for (int i = 0; i < allRooms.Count; i++)
        {
            var r = allRooms[i]; if (r.floorIdx != floor) continue;
            if (x >= r.rect.x && x < r.rect.x + r.rect.w && z >= r.rect.z && z < r.rect.z + r.rect.d) return true;
        }
        return false;
    }

    private float GetPrefabHeight(GameObject prefab)
    {
        if (!prefab) return 1f;
        var rend = prefab.GetComponentInChildren<Renderer>();
        if (rend) return rend.bounds.size.y;
        var col = prefab.GetComponentInChildren<Collider>();
        if (col) return col.bounds.size.y;
        return Mathf.Max(1f, prefab.transform.localScale.y);
    }

    private void PlaceLadderLink(Vector3 from, Vector3 to)
    {
        // Actual vertical span between floors
        float height = Mathf.Abs(to.y - from.y);

        // Find the room & push ladder to the nearest wall (x/z only)
        int f = WorldToFloorIndex(from);
        Room room = GetRoomContainingWorld(f, from);
        Vector3 local = transform.InverseTransformPoint(from);
        float yaw = ladderYaw;
        if (room != null)
        {
            float distW = local.x - room.rect.x;
            float distE = (room.rect.x + room.rect.w) - local.x;
            float distS = local.z - room.rect.z;
            float distN = (room.rect.z + room.rect.d) - local.z;
            float min = Mathf.Min(distW, distE, distS, distN);
            if (min == distS) { local.z = room.rect.z + ladderClearance; yaw = 0f; }
            else if (min == distN) { local.z = room.rect.z + room.rect.d - ladderClearance; yaw = 180f; }
            else if (min == distW) { local.x = room.rect.x + ladderClearance; yaw = 90f; }
            else { local.x = room.rect.x + room.rect.w - ladderClearance; yaw = -90f; }
        }

        // Convert the X/Z back to world (keep Y separate so we don't double-apply transform.position)
        Vector3 worldXZ = transform.TransformPoint(new Vector3(local.x, 0f, local.z));
        float yMid = (from.y + to.y) * 0.5f;

        // Spawn and orient ladder
        var go = Instantiate(ladderPrefab, transform);
        go.transform.SetPositionAndRotation(new Vector3(worldXZ.x, yMid, worldXZ.z), Quaternion.Euler(0f, yaw, 0f));

        // Scale Y so total bounds height == floor gap
        float prefabH = GetPrefabHeight(ladderPrefab);
        if (prefabH > 0.01f)
        {
            var ls = go.transform.localScale;
            go.transform.localScale = new Vector3(ls.x, height / prefabH, ls.z);
        }

        // Snap bottom of the ladder exactly to the lower floor plane
        float yMin = Mathf.Min(from.y, to.y);
        AlignBottomToWorldY(go, yMin);

        // Optional opening on the top floor (use the X/Z we computed above)
        if (cutTopOpening)
        {
            float yTop = Mathf.Max(from.y, to.y);
            RemoveCenteredFloorRectAtWorld(new Vector3(worldXZ.x, yTop, worldXZ.z), ladderOpeningSize.x, ladderOpeningSize.y);
        }
    }

    private Room GetRoomContainingWorld(int floor, Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        for (int i = 0; i < allRooms.Count; i++)
        {
            var r = allRooms[i]; if (r.floorIdx != floor) continue;
            if (local.x >= r.rect.x && local.x <= r.rect.x + r.rect.w && local.z >= r.rect.z && local.z <= r.rect.z + r.rect.d) return r;
        }
        return null;
    }

    private Vector3 ChooseStairDirectionFitting(Vector3 fromWorld, float neededRun, float margin, out float clearance)
    {
        int f = WorldToFloorIndex(fromWorld);
        Vector3 local = transform.InverseTransformPoint(fromWorld);
        Room room = GetRoomContainingWorld(f, fromWorld);
        if (room == null)
        {
            clearance = neededRun; return Vector3.forward; // fallback
        }

        float distPosX = (room.rect.x + room.rect.w) - local.x - margin;
        float distNegX = local.x - room.rect.x - margin;
        float distPosZ = (room.rect.z + room.rect.d) - local.z - margin;
        float distNegZ = local.z - room.rect.z - margin;

        var options = new List<(Vector3 dir, float dist)>
        {
            (Vector3.right, distPosX),
            (Vector3.left,  distNegX),
            (Vector3.forward, distPosZ),
            (Vector3.back, distNegZ)
        };
        // Remove negative clearances
        for (int i = options.Count - 1; i >= 0; i--) if (options[i].dist <= 0f) options.RemoveAt(i);
        options.Sort((a, b) => b.dist.CompareTo(a.dist));
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].dist >= neededRun)
            { clearance = options[i].dist; return options[i].dir; }
        }
        if (options.Count > 0) { clearance = options[0].dist; return options[0].dir; }
        clearance = neededRun; return Vector3.forward;
    }

    private void GetStairMetrics(GameObject prefab, out float rise, out float run, out float width)
    {
        rise = 1f; run = 1f; width = 1f;
        if (!prefab) return;
        var rend = prefab.GetComponentInChildren<Renderer>();
        if (rend)
        {
            var s = rend.bounds.size; // world size; we only need relative proportions
            rise = Mathf.Max(0.01f, s.y);
            run = Mathf.Max(0.01f, s.z);
            width = Mathf.Max(0.01f, s.x);
        }
        else
        {
            var col = prefab.GetComponentInChildren<Collider>();
            if (col)
            {
                var s = col.bounds.size;
                rise = Mathf.Max(0.01f, s.y);
                run = Mathf.Max(0.01f, s.z);
                width = Mathf.Max(0.01f, s.x);
            }
        }
    }

    private Vector3 ChooseStairDirection(Vector3 fromWorld, float neededRun)
    {
        // Find room containing the start point
        int f = WorldToFloorIndex(fromWorld);
        Vector3 local = transform.InverseTransformPoint(fromWorld);
        Room room = null;
        for (int i = 0; i < allRooms.Count; i++)
        {
            var r = allRooms[i]; if (r.floorIdx != f) continue;
            if (local.x >= r.rect.x && local.x <= r.rect.x + r.rect.w && local.z >= r.rect.z && local.z <= r.rect.z + r.rect.d) { room = r; break; }
        }
        if (room == null) return Vector3.forward; // fallback

        float distPosX = (room.rect.x + room.rect.w) - local.x;
        float distNegX = local.x - room.rect.x;
        float distPosZ = (room.rect.z + room.rect.d) - local.z;
        float distNegZ = local.z - room.rect.z;

        // Prefer the longest clearance that can fit the whole run, else the longest anyway
        var options = new List<(Vector3 dir, float dist)> {
            (Vector3.right, distPosX), (Vector3.left, distNegX), (Vector3.forward, distPosZ), (Vector3.back, distNegZ)
        };
        options.Sort((a, b) => b.dist.CompareTo(a.dist));
        foreach (var o in options) if (o.dist >= neededRun + 0.5f) return o.dir;
        return options[0].dir;
    }

    private void RemoveFloorRectAtWorld(Vector3 worldCenter, float halfSizeX, float halfSizeZ)
    {
        // Legacy: keep for compatibility (uses half sizes in world/grid units)
        int floor = WorldToFloorIndex(worldCenter);
        Vector3 local = transform.InverseTransformPoint(worldCenter);
        int minX = Mathf.FloorToInt(local.x - halfSizeX);
        int maxX = Mathf.FloorToInt(local.x + halfSizeX);
        int minZ = Mathf.FloorToInt(local.z - halfSizeZ);
        int maxZ = Mathf.FloorToInt(local.z + halfSizeZ);
        RemoveTilesInclusive(floor, minX, maxX, minZ, maxZ);
    }

    // New: remove an exact NxM patch of tiles centered on the given world position.
    private void RemoveCenteredFloorRectAtWorld(Vector3 worldCenter, int sizeX, int sizeZ)
    {
        int floor = WorldToFloorIndex(worldCenter);
        Vector3 local = transform.InverseTransformPoint(worldCenter);

        // Choose the tile directly under the position as the center
        int cx = Mathf.FloorToInt(local.x);
        int cz = Mathf.FloorToInt(local.z);

        // Symmetric span around the center tile (even sizes extend to +X/+Z)
        int left = (sizeX - 1) / 2;
        int right = sizeX / 2;
        int down = (sizeZ - 1) / 2;
        int up = sizeZ / 2;

        int minX = cx - left;
        int maxX = cx + right;
        int minZ = cz - down;
        int maxZ = cz + up;

        RemoveTilesInclusive(floor, minX, maxX, minZ, maxZ);
    }

    private void RemoveTilesInclusive(int floor, int minX, int maxX, int minZ, int maxZ)
    {
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                var key = new TileKey(floor, x, z);
                if (floorAt.TryGetValue(key, out var tile) && tile)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(tile);
                    else Destroy(tile);
#else
                    Destroy(tile);
#endif
                    floorAt.Remove(key);
                }
            }
        }
    }

    private void AlignBottomToWorldY(GameObject go, float worldY)
    {
        // Compute world bounds of GO and shift so minY == worldY
        var rends = go.GetComponentsInChildren<Renderer>();
        Bounds b; bool has = false;
        if (rends.Length > 0)
        {
            b = rends[0].bounds; has = true;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        }
        else
        {
            var cols = go.GetComponentsInChildren<Collider>();
            if (cols.Length == 0) return;
            b = cols[0].bounds; has = true;
            for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
        }
        if (has)
        {
            float dy = worldY - b.min.y;
            go.transform.position += new Vector3(0f, dy, 0f);
        }
    }

    private void PlaceWall(Transform parent, Vector3 localPos, bool alongX, int keyX, int keyZ)
    {
        var w = Instantiate(wallSegmentPrefab, parent);
        w.transform.localRotation = alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
        float halfH = GetPrefabHalfHeight(wallSegmentPrefab);
        w.transform.localPosition = localPos + Vector3.up * halfH;
        var key = new WallKey(keyX, keyZ, alongX, GetFloorIndexFromParent(parent));
        if (!wallAt.ContainsKey(key)) wallAt.Add(key, w);
    }

    private void OnDrawGizmosSelected()
    {
        foreach (var r in allRooms)
        {
            Gizmos.color = (r.type != null ? r.type.gizmoColor : Color.gray);
            Gizmos.DrawWireCube(r.Center(transform.position.y, floorHeight), new Vector3(r.rect.w, 0.1f, r.rect.d));
        }
        Gizmos.color = Color.white;
        foreach (var c in allCorridors) Gizmos.DrawLine(c.a, c.b);
        Gizmos.color = Color.cyan;
        for (int i = 0; i < ladderLinks.Count; i++) Gizmos.DrawLine(ladderLinks[i].from, ladderLinks[i].to);
    }
}
