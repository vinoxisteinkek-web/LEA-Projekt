using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public Transform head;
    public float playerSpeed = 5f;

    // =========================
    // TASCHENLAMPE
    // =========================

    public Light spotlight;

    // Sound für Taschenlampe
    public AudioSource flashlightSound;

    // Hinweis "F - Taschenlampe"
    public GameObject flashlightHint;

    public float flashlightHintDuration = 5f;


    // =========================
    // ANDERE WERTE
    // =========================

    public int dirtCount = 0;
    public int puddleCount = 0;
    public int trashCount = 0;

    private Rigidbody rb;


    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // =================================
        // TASCHENLAMPE AM ANFANG AUS
        // =================================

        if (spotlight != null)
        {
            spotlight.enabled = false;
        }

        // Hinweis am Anfang verstecken
        if (flashlightHint != null)
        {
            flashlightHint.SetActive(false);
        }
    }


    void Update()
    {
        // =================================
        // BEWEGUNG
        // =================================

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = head.right * horizontal + head.forward * vertical;

        // Y-Richtung entfernen
        direction.y = 0f;

        // Diagonale Bewegung nicht schneller
        direction = direction.normalized;

        // Bewegung setzen
        rb.linearVelocity = new Vector3(
            direction.x * playerSpeed,
            rb.linearVelocity.y,
            direction.z * playerSpeed
        );


        // =================================
        // TASCHENLAMPE
        // =================================

        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlight();
        }
    }


    // =================================
    // TASCHENLAMPE AN/AUS
    // =================================

    private void ToggleFlashlight()
    {
        if (spotlight == null)
            return;

        // Taschenlampe umschalten
        spotlight.enabled = !spotlight.enabled;

        // Sound abspielen
        if (flashlightSound != null)
        {
            flashlightSound.Play();
        }
    }


    // =================================
    // HINWEIS NACH DEM HAUS
    // =================================

    public void ShowFlashlightHint()
    {
        if (flashlightHint == null)
            return;

        StartCoroutine(FlashlightHintCoroutine());
    }


    private System.Collections.IEnumerator FlashlightHintCoroutine()
    {
        flashlightHint.SetActive(true);

        yield return new WaitForSeconds(flashlightHintDuration);

        flashlightHint.SetActive(false);
    }
}