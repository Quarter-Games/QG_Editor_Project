using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public string keyId;

    private void OnTriggerEnter(Collider other)
    {
        var inv = other.GetComponent<PlayerInventory>();
        if (inv)
        {
            inv.AddKey(keyId);
            Destroy(gameObject);
        }
    }
}
