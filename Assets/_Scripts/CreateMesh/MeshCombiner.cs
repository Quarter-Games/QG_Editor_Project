using UnityEngine;

public class MeshCombiner : MonoBehaviour
{
    // Call this after placing all room parts as children of this GameObject
    public void CombineMeshesInRoom()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }

        // Create a new mesh
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);

        // Create mesh filter and renderer for the combined mesh
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = combinedMesh;

        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = meshFilters[0].GetComponent<MeshRenderer>().sharedMaterial;

        // Optional: remove child objects
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}