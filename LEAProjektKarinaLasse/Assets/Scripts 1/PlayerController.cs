using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public Transform head;
    public float playerSpeed = 5f;
    public Light spotlight;
    public TextMeshProUGUI dirtCountText;
    public TextMeshProUGUI puddleCountText;

    public int dirtCount = 0;
    public int puddleCount = 0;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        dirtCountText.text = dirtCount.ToString();
        puddleCountText.text = puddleCount.ToString();
    }

    void Update()
    {
        if (dirtCountText != null)
        {
            dirtCountText.text = "DirtCount: " + dirtCount.ToString();
        }
        if (puddleCountText != null)
        {
            puddleCountText.text = "PuddleCount: " + puddleCount.ToString();
        }

        // Eingabe holen
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Richtung anhand der Kamera
        Vector3 direction = head.right * horizontal + head.forward * vertical;

        // Y-Richtung entfernen
        direction.y = 0f;

        // Diagonale Bewegung nicht schneller machen
        direction = direction.normalized;

        // Bewegung setzen
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