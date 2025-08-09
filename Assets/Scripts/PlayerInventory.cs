using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private HashSet<string> keys = new();

    public void AddKey(string id) { if (!string.IsNullOrEmpty(id)) keys.Add(id); }
    public bool HasKey(string id) => !string.IsNullOrEmpty(id) && keys.Contains(id);
}
