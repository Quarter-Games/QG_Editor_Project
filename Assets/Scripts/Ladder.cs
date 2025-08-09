using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Ladder : MonoBehaviour
{
    [Tooltip("Bottom world anchor of the ladder. Auto-created on Reset if missing.")]
    public Transform bottom;

    [Tooltip("Top world anchor of the ladder. Auto-created on Reset if missing.")]
    public Transform top;

    [Tooltip("Cylinder radius used to keep the climber aligned to the ladder.")]
    public float radius = 0.4f;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (bottom == null)
        {
            bottom = new GameObject("Bottom").transform;
            bottom.SetParent(transform, false);
            bottom.localPosition = Vector3.down * 0.5f;
        }
        if (top == null)
        {
            top = new GameObject("Top").transform;
            top.SetParent(transform, false);
            top.localPosition = Vector3.up * 0.5f;
        }
    }

    public Vector3 Up => (top.position - bottom.position).normalized;
    public float Height => Vector3.Distance(bottom.position, top.position);

    // Closest point on the ladder segment, with a small lateral clamp so the player stays on it.
    public Vector3 ClosestPointOnLadder(Vector3 worldPos)
    {
        Vector3 a = bottom.position;
        Vector3 b = top.position;
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(worldPos - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
        Vector3 p = a + ab * t;

        // pull player toward the ladder axis, but allow slight sideways radius
        Vector3 off = worldPos - p;
        Vector3 offParallel = Vector3.Project(off, ab);
        Vector3 offPerp = off - offParallel;
        if (offPerp.sqrMagnitude > radius * radius)
            offPerp = offPerp.normalized * radius;

        return p + offPerp;
    }

    void OnTriggerEnter(Collider other)
    {
        var climber = other.GetComponent<LadderClimber>();
        if (climber != null) climber.Attach(this);
    }

    void OnTriggerExit(Collider other)
    {
        var climber = other.GetComponent<LadderClimber>();
        if (climber != null) climber.Detach(this);
    }
}
