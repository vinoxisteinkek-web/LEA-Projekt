using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform head;
    public float playerSpeed = 5f;
    public float playerAcceleration = 10f;
    public Light spotlight;
    private Rigidbody rb;
    private Vector3 direction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        direction = Input.GetAxis("Horizontal") * head.right + Input.GetAxis("Vertical") * head.forward;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, direction.normalized * playerSpeed + rb.linearVelocity.y * Vector3.up, playerAcceleration * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.F))
        {
            spotlight.enabled = !spotlight.enabled;
        }
    }
}
