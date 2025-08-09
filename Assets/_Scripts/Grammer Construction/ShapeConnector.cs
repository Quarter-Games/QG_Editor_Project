using UnityEngine;
using UnityEngine.ProBuilder;

public class ShapeConnector : MonoBehaviour
{
    public Transform EdgeA;
    public Transform EdgeB;

    // Direction vector between edges (used for alignment)
    public Vector3 ConnectionDirection => (EdgeB.position - EdgeA.position).normalized;

    // Returns the world positions of the two connection points
    public Vector3[] GetConnectionPoints()
    {
        return new Vector3[] { EdgeA.position, EdgeB.position };
    }
}