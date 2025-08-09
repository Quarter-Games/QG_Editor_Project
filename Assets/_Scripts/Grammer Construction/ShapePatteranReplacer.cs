using UnityEngine;

public class ShapePatternReplacer : MonoBehaviour
{
    public ShapeConnector Pattern;          // The shape to detect (reference ProBuilder mesh)
    public ShapeConnector ReplacementPrefab; // The prefab to insert

    [Tooltip("How close edge lengths must match to count as a pattern match")]
    public float LengthTolerance = 0.5f;

    [Tooltip("How closely direction must match (dot product, 1 = perfect alignment)")]
    public float DirectionTolerance = 0.95f;

    [ContextMenu("TryReplaceAll")]
    public void TryReplaceAll()
    {
        ShapeConnector[] connectors = FindObjectsOfType<ShapeConnector>();

        foreach (var connector in connectors)
        {
            if (connector == Pattern) continue; // skip the pattern reference itself

            if (IsMatch(connector, Pattern))
            {
                ReplaceShape(connector);
            }
        }
    }

    bool IsMatch(ShapeConnector target, ShapeConnector reference)
    {
        // Compare edge spacing
        float targetDist = Vector3.Distance(target.EdgeA.position, target.EdgeB.position);
        float refDist = Vector3.Distance(reference.EdgeA.position, reference.EdgeB.position);
        if (Mathf.Abs(targetDist - refDist) > LengthTolerance)
            return false;

        // Compare direction
        Vector3 targetDir = target.ConnectionDirection.normalized;
        Vector3 refDir = reference.ConnectionDirection.normalized;
        float dot = Vector3.Dot(targetDir, refDir);
        if (dot < DirectionTolerance)
            return false;

        return true;
    }

    void ReplaceShape(ShapeConnector target)
    {
        Vector3[] targetPoints = target.GetConnectionPoints();
        Vector3 targetDir = target.ConnectionDirection.normalized;

        // Spawn replacement
        ShapeConnector replacement = Instantiate(ReplacementPrefab, Vector3.zero, Quaternion.identity);

        // Align replacement's edge direction and position to target
        Vector3[] replPoints = replacement.GetConnectionPoints();
        Vector3 replDir = replacement.ConnectionDirection.normalized;

        // Compute rotation to align directions
        Quaternion rotation = Quaternion.FromToRotation(replDir, targetDir);

        // Move replacement so EdgeA matches
        Vector3 offset = targetPoints[0] - (rotation * replPoints[0]);
        replacement.transform.rotation = rotation;
        replacement.transform.position += offset;

        // Remove original target
        Destroy(target.gameObject);
    }
}
