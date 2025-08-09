using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LadderClimber : MonoBehaviour
{
    [Header("Climbing")]
    public float climbSpeed = 3f;
    public float stepOffBoost = 2f;    // small forward nudge when leaving the top
    public KeyCode detachKey = KeyCode.Space;

    Rigidbody rb;
    CharacterController cc;
    Ladder current;
    bool isClimbing;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CharacterController>();
    }

    public void Attach(Ladder ladder)
    {
        current = ladder;
        isClimbing = true;

        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.useGravity = false;
        }
    }

    public void Detach(Ladder ladder)
    {
        if (current != ladder) return;
        Detach();
    }

    void Detach()
    {
        if (rb) rb.useGravity = true;
        isClimbing = false;
        current = null;
    }

    void Update()
    {
        if (!isClimbing || current == null)
            return;

        // Input: W/S or Up/Down to climb
        float v = Input.GetAxisRaw("Vertical"); // -1..1
        Vector3 delta = current.Up * (v * climbSpeed * Time.deltaTime);

        // Keep the player constrained to the ladder cylinder & segment
        Vector3 target = current.ClosestPointOnLadder(transform.position + delta);

        // Clamp to segment ends
        Vector3 a = current.bottom.position;
        Vector3 b = current.top.position;
        float t = Mathf.Clamp01(Vector3.Dot(target - a, (b - a)) / Mathf.Max(0.0001f, (b - a).sqrMagnitude));
        target = Vector3.Lerp(a, b, t);

        MoveTo(target);

        // Manual detach
        if (Input.GetKeyDown(detachKey))
            Detach();

        // Step-off helper when reaching the very top
        if (Mathf.Approximately(t, 1f))
        {
            // small nudge away from ladder
            Vector3 away = Vector3.ProjectOnPlane(transform.forward, current.Up).normalized;
            if (away.sqrMagnitude > 0.01f)
            {
                if (cc) cc.Move(away * stepOffBoost * Time.deltaTime);
                else if (rb) rb.MovePosition(transform.position + away * stepOffBoost * Time.deltaTime);
                else transform.position += away * stepOffBoost * Time.deltaTime;
            }
        }
    }

    void MoveTo(Vector3 pos)
    {
        if (cc)
        {
            Vector3 delta = pos - transform.position;
            cc.Move(delta);
        }
        else if (rb)
        {
            rb.MovePosition(pos);
        }
        else
        {
            transform.position = pos;
        }
    }
}
