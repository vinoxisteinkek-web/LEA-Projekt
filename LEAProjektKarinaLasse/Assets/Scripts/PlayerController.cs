using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform head;
    public float playerSpeed = 5f;
    public float playerAcceleration = 10f;
    public Light spotlight;
    private Rigidbody rb;
    private Vector3 direction;
    private Vector3 targetVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        direction = Input.GetAxis("Horizontal") * head.right + Input.GetAxis("Vertical") * head.forward;
        
        // Zielgeschwindigkeit basierend auf Input berechnen
        targetVelocity = direction.normalized * playerSpeed + rb.linearVelocity.y * Vector3.up;
        
        // Nur wenn es Input gibt, zur Zielgeschwindigkeit interpolieren
        // Ansonsten zur Null-Velocity (kein horizontaler Input = stillstehen)
        if (direction.magnitude > 0.01f)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, playerAcceleration * Time.deltaTime);
        }
        else
        {
            // Kein Input: Horizontal abbremsen, aber Gravity beibehalten
            Vector3 stoppedVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, stoppedVelocity, playerAcceleration * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            spotlight.enabled = !spotlight.enabled;
        }
    }
}
