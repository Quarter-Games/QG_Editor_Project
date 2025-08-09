using System.Collections.Generic;
using UnityEngine;

public class BSPFloor : MonoBehaviour
{
    [System.Serializable]
    public class Node
    {
        public RectInt rect;
        public Node left, right;
        public RectInt? room;              
        public Node(RectInt r) => rect = r;
        public bool IsLeaf => left == null && right == null;
    }

    public List<RectInt> Rooms { get; private set; } = new();

    public void Generate(int width, int depth, int maxDepth, int corridorWidth)
    {
        Rooms.Clear();
        Node root = new Node(new RectInt(0, 0, width, depth));
        SplitRecursive(root, maxDepth);
        CreateRooms(root);
        BuildGeometry(width, depth, corridorWidth);
    }

    #region --- BSP core ---
    bool SplitRecursive(Node node, int depth)
    {
        if (depth <= 0 || node.rect.width < 10 || node.rect.height < 10)
            return false;

        bool splitHoriz = node.rect.width < node.rect.height;
        int max = (splitHoriz ? node.rect.height : node.rect.width) - 4;
        int split = Random.Range(4, max);

        if (splitHoriz)
        {
            node.left = new Node(new RectInt(node.rect.x, node.rect.y, node.rect.width, split));
            node.right = new Node(new RectInt(node.rect.x, node.rect.y + split, node.rect.width, node.rect.height - split));
        }
        else
        {
            node.left = new Node(new RectInt(node.rect.x, node.rect.y, split, node.rect.height));
            node.right = new Node(new RectInt(node.rect.x + split, node.rect.y, node.rect.width - split, node.rect.height));
        }
        SplitRecursive(node.left, depth - 1);
        SplitRecursive(node.right, depth - 1);
        return true;
    }

    void CreateRooms(Node node)
    {
        if (node.IsLeaf)
        {
            int w = Random.Range(4, node.rect.width - 1);
            int h = Random.Range(4, node.rect.height - 1);
            int x = Random.Range(1, node.rect.width - w);
            int y = Random.Range(1, node.rect.height - h);

            node.room = new RectInt(node.rect.x + x, node.rect.y + y, w, h);
            Rooms.Add(node.room.Value);
        }
        else
        {
            if (node.left != null) CreateRooms(node.left);
            if (node.right != null) CreateRooms(node.right);
        }
    }
    #endregion

    #region --- Mesh / debug ---
    void BuildGeometry(int w, int h, int corridor)
    {
        foreach (var r in Rooms)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(r.width, 3, r.height);
            go.transform.localPosition = new Vector3(r.x + r.width / 2f - w / 2f,
                                                     1.5f,
                                                     r.y + r.height / 2f - h / 2f);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var r in Rooms)
        {
            Vector3 center = new(r.x + r.width / 2f, 1, r.y + r.height / 2f);
            Gizmos.DrawWireCube(center, new(r.width, 2, r.height));
        }
    }
#endif
    #endregion
}
