using UnityEngine;

public class Door : MonoBehaviour
{
    public bool locked = true;
    public string keyId;
    public float openDistance = 2f;

    public void TryOpen(PlayerInventory inv)
    {
        if (!locked) { Open(); return; }
        if (inv && inv.HasKey(keyId)) { locked = false; Open(); }
        else { Debug.Log("Door is locked."); }
    }

    private void Open()
    {
        // simplest: disable collider & slide door up slightly
        var col = GetComponent<Collider>(); if (col) col.enabled = false;
        transform.position += Vector3.up * 2f;
    }
}
