using UnityEngine;

public class SawbladeMovementLocal : MonoBehaviour
{
    public float speed = 2.0f;
    public float distance = 5.0f;
    public float rotationSpeed = 360.0f;

    private Vector3 startLocalPosition;
    private int direction = 1;

    void Start()
    {
        startLocalPosition = transform.localPosition;
    }

    void Update()
    {
        float newX = startLocalPosition.x + Mathf.PingPong(Time.time * speed, distance * 2) - distance;

        transform.localPosition = new Vector3(newX, transform.localPosition.y, transform.localPosition.z);

        transform.Rotate(0, 0, rotationSpeed * direction * Time.deltaTime);

        if (newX >= startLocalPosition.x + distance - 0.1f && direction == 1)
        {
            direction = -1;
        }
        else if (newX <= startLocalPosition.x - distance + 0.1f && direction == -1)
        {
            direction = 1;
        }
    }
}