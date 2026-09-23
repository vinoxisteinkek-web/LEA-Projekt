using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform head;
    public float playerSpeed = 5f;
    public Light spotlight;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Eingabe holen
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Richtung anhand der Kamera
        Vector3 direction = head.right * horizontal + head.forward * vertical;

        // Y-Richtung entfernen, damit wir nicht nach oben/unten laufen
        direction.y = 0f;

        // Diagonale Bewegung nicht schneller machen
        direction = direction.normalized;

        // Bewegung direkt setzen
        rb.linearVelocity = new Vector3(
            direction.x * playerSpeed,
            rb.linearVelocity.y,
            direction.z * playerSpeed
        );

        // Taschenlampe mit F
        if (Input.GetKeyDown(KeyCode.F))
        {
            spotlight.enabled = !spotlight.enabled;
        }
        
    }

   
}
