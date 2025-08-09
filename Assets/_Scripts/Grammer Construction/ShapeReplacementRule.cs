
using UnityEngine;

public class ShapeReplacementRule : IGrammarRule
{
    private ShapeConnector pattern;
    private ShapeConnector replacementPrefab;
    private float lengthTolerance;
    private float directionTolerance;
    
    public string Name { get; private set; }

    public ShapeReplacementRule(
        ShapeConnector pattern,
        ShapeConnector replacementPrefab,
        string name = "ShapeReplacement",
        float lengthTolerance = 0.1f,
        float directionTolerance = 0.95f)
    {
        this.pattern = pattern;
        this.replacementPrefab = replacementPrefab;
        this.Name = name;
        this.lengthTolerance = lengthTolerance;
        this.directionTolerance = directionTolerance;
    }

    // Implement CanApply method required by the interface
    public bool CanApply(IGrammarTarget target)
    {
        // You may need to adapt this based on your target type
        // This is just an example assuming target provides access to ShapeConnectors
        ShapeConnector[] allShapes = GameObject.FindObjectsOfType<ShapeConnector>();
        
        foreach (var shape in allShapes)
        {
           
            
            if (MatchesPattern(shape))
            {
                return true;
            }
        }
        
        return false;
    }

    // Implement Apply method required by the interface
    public void Apply(IGrammarTarget target)
    {
        // Similar adaptation needed based on your target type
        ShapeConnector[] allShapes = GameObject.FindObjectsOfType<ShapeConnector>();
        
        foreach (var shape in allShapes)
        {
            if (shape == pattern) continue;
            
            if (MatchesPattern(shape))
            {
                ReplaceShape(shape);
                break; // Or continue if you want to replace all matching shapes
            }
        }
    }
    
    private bool MatchesPattern(ShapeConnector target)
    {
        // Compare edge spacing
        float targetDist = Vector3.Distance(target.EdgeA.position, target.EdgeB.position);
        float refDist = Vector3.Distance(pattern.EdgeA.position, pattern.EdgeB.position);
        if (Mathf.Abs(targetDist - refDist) > lengthTolerance)
            return false;

        // Compare direction
        Vector3 targetDir = (target.EdgeB.position - target.EdgeA.position).normalized;
        Vector3 refDir = (pattern.EdgeB.position - pattern.EdgeA.position).normalized;
        float dot = Vector3.Dot(targetDir, refDir);
        if (dot < directionTolerance)
            return false;

        return true;
    }

    // Renamed from Apply to avoid confusion with interface method
    private void ReplaceShape(ShapeConnector target)
    {
        Vector3[] targetPoints = { target.EdgeA.position, target.EdgeB.position };
        Vector3 targetDir = (target.EdgeB.position - target.EdgeA.position).normalized;

        // Spawn replacement
        ShapeConnector newShape = GameObject.Instantiate(replacementPrefab, Vector3.zero, Quaternion.identity);

        Vector3[] replPoints = { newShape.EdgeA.position, newShape.EdgeB.position };
        Vector3 replDir = (newShape.EdgeB.position - newShape.EdgeA.position).normalized;

        // Rotate to align edge direction
        Quaternion rotation = Quaternion.FromToRotation(replDir, targetDir);
        newShape.transform.rotation = rotation;

        // Position so EdgeA aligns
        Vector3 offset = targetPoints[0] - (rotation * replPoints[0]);
        newShape.transform.position += offset;

        GameObject.Destroy(target.gameObject);
    }
}