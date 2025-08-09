using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

[ExecuteAlways]
public class DungeonGenerator : MonoBehaviour
{
    [Header("Seed & Scale")]
    public int seed = 12345;
    public float cellSize = 1f;

    [Header("Dungeon Size")]
    public int dungeonWidth = 64;
    public int dungeonDepth = 64;
    public int floors = 2;
    public float floorHeight = 6f;

    [Header("BSP")]
    public int maxSplitDepth = 4;
    public int minLeafSize = 12; 
    public int corridorWidth = 2;

    [Header("Rooms & Quest")]
    public List<RoomType> roomTypes;
    public int numLockedRooms = 1;
    public int numTreasureRooms = 1;

    [Header("Prefabs")]
    public GameObject floorTilePrefab;
    public GameObject wallSegmentPrefab;
    public GameObject closedDoorPrefab;
    public GameObject keyPickupPrefab;
    public GameObject stairsUpPrefab;
    public GameObject stairsDownPrefab;

    [Header("NavMesh")]
    public NavMeshSurface navSurface;

    [Header("Runtime")]
    public bool autoGenerateOnStart = false;
    public bool clearBeforeGenerate = true;

    // Internal structures
    private System.Random rng;
    private List<Room> allRooms = new();
    private List<Corridor> allCorridors = new();
    private List<(Vector3 from, Vector3 to)> stairLinks = new();
    private Dictionary<Room, GameObject> roomRoots = new();

    #region Data
    private struct RectI { public int x, z, w, d; public RectI(int x, int z, int w, int d) { this.x = x; this.z = z; this.w = w; this.d = d; } }
    private class BSPNode
    {
        public RectI area;
        public BSPNode left, right;
        public RectI? roomRect;
        public int floorIdx;
        public BSPNode(RectI r, int floor) { area = r; floorIdx = floor; }
    }
    private class Room
    {
        public RectI rect;
        public int floorIdx;
        public RoomType type;
        public string keyId; // for Key/Locked mapping
        public Vector3 Center(float yBase, float hPerFloor)
            => new Vector3((rect.x + rect.w / 2f), yBase + floorIdx * hPerFloor, (rect.z + rect.d / 2f));
    }
    private struct Corridor { public Vector3 a, b; public int floor; }
    #endregion

    void Start()
    {
        if (Application.isPlaying && autoGenerateOnStart)
            Generate();
    }

    [ContextMenu("Generate Dungeon")]
    public void Generate()
    {
        if (!ValidatePrefabs()) { Debug.LogWarning("Assign all required prefabs & NavMeshSurface."); return; }
        rng = new System.Random(seed);

        if (clearBeforeGenerate) ClearChildren();

        allRooms.Clear(); allCorridors.Clear(); stairLinks.Clear(); roomRoots.Clear();

        // 1) BSP per floor -> rooms
        var floorsNodes = new List<BSPNode>();
        for (int f = 0; f < floors; f++)
        {
            RectI full = new RectI(0, 0, dungeonWidth, dungeonDepth);
            var root = new BSPNode(full, f);
            SplitRecursive(root, 0);
            CreateRoomsFromLeaves(root);
            floorsNodes.Add(root);
        }

        // 2) Connect rooms per floor with simple nearest-neighbour corridors
        foreach (var root in floorsNodes)
            ConnectFloor(root);

        // 3) Cross-floor connections (stairs) – pair a random room on floor F with closest room on F+1
        for (int f = 0; f < floors - 1; f++)
            LinkFloorsWithStairs(f, f + 1);

        // 4) Instantiate geometry (rooms, walls, corridors, doors, keys)
        BuildGeometry();

        // 5) Build NavMesh
        if (navSurface) navSurface.BuildNavMesh();
    }

    private bool ValidatePrefabs()
    {
        return floorTilePrefab && wallSegmentPrefab && closedDoorPrefab && keyPickupPrefab
               && stairsUpPrefab && stairsDownPrefab && navSurface;
    }

    private void ClearChildren()
    {
        var toDestroy = new List<GameObject>();
        foreach (Transform c in transform) toDestroy.Add(c.gameObject);
        while (toDestroy.Count > 0)
        {
            var go = toDestroy[0]; toDestroy.RemoveAt(0);
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.Undo.DestroyObjectImmediate(go);
            else Destroy(go);
#else
            DestroyImmediate(go);
#endif
        }
    }

    private void SplitRecursive(BSPNode n, int depth)
    {
        if (depth >= maxSplitDepth || n.area.w < minLeafSize * 2 || n.area.d < minLeafSize * 2)
            return;

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
            // pick a room size within the leaf
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
        // Simple weighted pick: prefer Normal; enforce floor constraints.
        for (int i = 0; i < 10; i++)
        {
            var tryRt = roomTypes[rng.Next(roomTypes.Count)];
            if (floor >= tryRt.minFloor && floor <= tryRt.maxFloor) return tryRt;
        }
        return roomTypes[0];
    }

    private void ConnectFloor(BSPNode root)
    {
        // naive: connect each room to its nearest neighbour on the same floor
        var floorRooms = allRooms.FindAll(r => r.floorIdx == root.floorIdx);
        for (int i = 0; i < floorRooms.Count; i++)
        {
            var a = floorRooms[i];
            float best = float.MaxValue; Room bestB = null;
            for (int j = 0; j < floorRooms.Count; j++)
            {
                if (i == j) continue;
                var b = floorRooms[j];
                var da = a.Center(transform.position.y, floorHeight);
                var db = b.Center(transform.position.y, floorHeight);
                float d = Vector3.SqrMagnitude(da - db);
                if (d < best) { best = d; bestB = b; }
            }
            if (bestB != null)
            {
                allCorridors.Add(new Corridor
                {
                    a = a.Center(transform.position.y, floorHeight),
                    b = bestB.Center(transform.position.y, floorHeight),
                    floor = a.floorIdx
                });
            }
        }
    }

    private void LinkFloorsWithStairs(int fA, int fB)
    {
        var roomsA = allRooms.FindAll(r => r.floorIdx == fA);
        var roomsB = allRooms.FindAll(r => r.floorIdx == fB);
        if (roomsA.Count == 0 || roomsB.Count == 0) return;

        // choose a random room on A, link to closest on B
        var ra = roomsA[rng.Next(roomsA.Count)];
        Vector3 ca = ra.Center(transform.position.y, floorHeight);
        float best = float.MaxValue; Room bestB = null;
        foreach (var rb in roomsB)
        {
            var cb = rb.Center(transform.position.y, floorHeight);
            float d = Vector3.SqrMagnitude(new Vector3(cb.x, 0, cb.z) - new Vector3(ca.x, 0, ca.z));
            if (d < best) { best = d; bestB = rb; }
        }
        if (bestB != null) stairLinks.Add((ca, bestB.Center(transform.position.y, floorHeight)));
    }

    private void BuildGeometry()
    {
        // simple: one root per floor
        var floorRoots = new List<Transform>();
        for (int f = 0; f < floors; f++)
        {
            var root = new GameObject($"Floor_{f}").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0, transform.position.y + f * floorHeight, 0);
            floorRoots.Add(root);
        }

        // rooms
        foreach (var r in allRooms)
        {
            Transform parent = floorRoots[r.floorIdx];
            var roomGo = new GameObject($"{r.type?.kind.ToString() ?? "Room"}_{r.floorIdx}").transform;
            roomGo.SetParent(parent, false);
            roomRoots[r] = roomGo.gameObject;

            // build floor plane with tiled cubes
            for (int x = 0; x < r.rect.w; x++)
                for (int z = 0; z < r.rect.d; z++)
                {
                    Vector3 pos = new Vector3(r.rect.x + x + 0.5f, 0, r.rect.z + z + 0.5f);
                    var tile = Instantiate(floorTilePrefab, parent);
                    tile.transform.localPosition = pos;
                }

            // perimeter walls (coarse, every 1 meter)
            for (int x = 0; x < r.rect.w; x++)
            {
                PlaceWall(parent, new Vector3(r.rect.x + x + 0.5f, 0, r.rect.z - 0.5f));
                PlaceWall(parent, new Vector3(r.rect.x + x + 0.5f, 0, r.rect.z + r.rect.d + 0.5f));
            }
            for (int z = 0; z < r.rect.d; z++)
            {
                PlaceWall(parent, new Vector3(r.rect.x - 0.5f, 0, r.rect.z + z + 0.5f));
                PlaceWall(parent, new Vector3(r.rect.x + r.rect.w + 0.5f, 0, r.rect.z + z + 0.5f));
            }

            // decoration via grammar rules
            if (r.type && r.type.decoration) DecorateRoom(r, parent);
        }

        // corridors (as strips of floor tiles)
        foreach (var c in allCorridors)
        {
            var parent = floorRoots[c.floor];
            DrawCorridor(parent, c.a, c.b);
        }

        // doors & keys (simple placement: if room is Locked, put a door at its entrance; if Key, place a pickup)
        foreach (var r in allRooms)
        {
            if (r.type == null) continue;
            if (r.type.kind == RoomKind.Locked)
            {
                // place a door at front wall center
                Vector3 front = new Vector3(r.rect.x + r.rect.w / 2f, 0, r.rect.z - 0.5f);
                var d = Instantiate(closedDoorPrefab, roomRoots[r].transform.parent);
                d.transform.localPosition = front;
                var door = d.GetComponent<Door>(); if (door) { door.keyId = r.keyId; }
            }
            if (r.type.kind == RoomKind.Key)
            {
                Vector3 kp = new Vector3(r.rect.x + r.rect.w / 2f, 0.6f, r.rect.z + r.rect.d / 2f);
                var k = Instantiate(keyPickupPrefab, roomRoots[r].transform.parent);
                k.transform.localPosition = kp;
                var item = k.GetComponent<ItemPickup>(); if (item) { item.keyId = r.keyId; }
            }
        }

        // stairs between floors
        foreach (var link in stairLinks)
        {
            // place stairs up at 'from', stairs down at 'to'
            var up = Instantiate(stairsUpPrefab, transform);
            up.transform.position = link.from;
            var down = Instantiate(stairsDownPrefab, transform);
            down.transform.position = link.to;
        }
    }

    private void PlaceWall(Transform parent, Vector3 localPos)
    {
        var w = Instantiate(wallSegmentPrefab, parent);
        w.transform.localPosition = localPos + Vector3.up * (wallSegmentPrefab.transform.localScale.y * 0.5f);
    }

    private void DrawCorridor(Transform parent, Vector3 a, Vector3 b)
    {
        // L-shaped corridor along grid between centers
        Vector3 p = a; p.y = 0; Vector3 q = b; q.y = 0;
        int x0 = Mathf.RoundToInt(p.x), z0 = Mathf.RoundToInt(p.z);
        int x1 = Mathf.RoundToInt(q.x), z1 = Mathf.RoundToInt(q.z);

        // horizontal then vertical
        int dx = x1 >= x0 ? 1 : -1;
        for (int x = x0; x != x1; x += dx)
        {
            for (int w = -corridorWidth / 2; w <= corridorWidth / 2; w++)
            {
                var t = Instantiate(floorTilePrefab, parent);
                t.transform.localPosition = new Vector3(x + 0.5f, 0, z0 + w + 0.5f);
            }
        }
        int dz = z1 >= z0 ? 1 : -1;
        for (int z = z0; z != z1; z += dz)
        {
            for (int w = -corridorWidth / 2; w <= corridorWidth / 2; w++)
            {
                var t = Instantiate(floorTilePrefab, parent);
                t.transform.localPosition = new Vector3(x1 + w + 0.5f, 0, z + 0.5f);
            }
        }
    }

    private void DecorateRoom(Room r, Transform parent)
    {
        var prof = r.type.decoration;
        float step = Mathf.Max(0.5f, prof.sampleStep);

        for (float x = r.rect.x + 1; x < r.rect.x + r.rect.w - 1; x += step)
            for (float z = r.rect.z + 1; z < r.rect.z + r.rect.d - 1; z += step)
            {
                Vector3 p = new Vector3(x + 0.5f, 0, z + 0.5f);
                foreach (var rule in prof.rules)
                {
                    if (rule.prefab == null) continue;
                    if (Random.value <= rule.probability)
                    {
                        var go = Instantiate(rule.prefab, parent);
                        go.transform.localPosition = p;
                        break;
                    }
                }
            }
    }

    // ---- Gizmos ----
    private void OnDrawGizmosSelected()
    {
        // rooms
        foreach (var r in allRooms)
        {
            Gizmos.color = r.type ? r.type.gizmoColor : Color.gray;
            Vector3 center = r.Center(transform.position.y, floorHeight);
            Vector3 size = new Vector3(r.rect.w, 0.1f, r.rect.d);
            Gizmos.DrawWireCube(center, size);
        }
        // corridors
        Gizmos.color = Color.white;
        foreach (var c in allCorridors) Gizmos.DrawLine(c.a, c.b);
        // stairs
        Gizmos.color = Color.cyan;
        foreach (var s in stairLinks) Gizmos.DrawLine(s.from, s.to);
    }
}
