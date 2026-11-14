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
    // SCHRITTE
    // =========================

    [Header("Schritte")]
    // Lauf-Sound (wird in einer Schleife abgespielt, solange der Spieler läuft)
    public AudioClip walkingClip;

    [Range(0f, 1f)]
    public float walkingVolume = 0.6f;

    // Wie schnell der Sound ein-/ausgeblendet wird (verhindert Knacken)
    public float footstepFadeSpeed = 8f;

    private AudioSource footstepSource;


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

        SetupFootsteps();
    }


    // Eingabe wird in Update gelesen, die Physik in FixedUpdate gesetzt
    private Vector3 moveDirection;

    void Update()
    {
        // =================================
        // BEWEGUNG (Eingabe lesen)
        // =================================

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = head.right * horizontal + head.forward * vertical;

        // Y-Richtung entfernen
        direction.y = 0f;

        // Diagonale Bewegung nicht schneller
        moveDirection = direction.normalized;


        // =================================
        // SCHRITTE
        // =================================

        UpdateFootsteps();


        // =================================
        // TASCHENLAMPE
        // =================================

        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlight();
        }
    }


    void FixedUpdate()
    {
        // =================================
        // BEWEGUNG (Physik)
        // Rigidbody-Werte nur im Physik-Takt setzen.
        // Zusammen mit Interpolation am Rigidbody = flüssige Kamera.
        // =================================

        float verticalVelocity = rb.linearVelocity.y;

        // Der Spieler springt nie. Schwung nach oben entsteht nur, wenn die Kapsel
        // über eine Kante (Bordstein, Stufe) rutscht -> abschneiden, sonst hüpft er.
        if (verticalVelocity > 0f)
            verticalVelocity = 0f;

        // Beim Herunterlaufen von einer Kante am Boden bleiben, statt kurz zu schweben.
        bool grounded = IsGrounded();
        bool snapped = false;
        if (!grounded && wasGroundedLastStep && GroundCheck(groundSnapDistance))
        {
            verticalVelocity = Mathf.Min(verticalVelocity, -groundSnapSpeed);
            snapped = true;
        }
        wasGroundedLastStep = grounded || snapped;

        rb.linearVelocity = new Vector3(
            moveDirection.x * playerSpeed,
            verticalVelocity,
            moveDirection.z * playerSpeed
        );
    }


    // =================================
    // SCHRITTE
    // =================================

    private void SetupFootsteps()
    {
        if (walkingClip == null)
            return;

        // Eigene AudioSource nur für die Schritte,
        // damit sich nichts mit anderen Sounds (z. B. Stimme) überschneidet
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.clip = walkingClip;
        footstepSource.loop = true;
        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = 0f; // 2D – kommt vom Spieler selbst
        footstepSource.volume = 0f;
    }

    private void UpdateFootsteps()
    {
        if (footstepSource == null || rb == null)
            return;

        // Nur wenn der Spieler laufen will, sich wirklich bewegt
        // (nicht gegen eine Wand drückt) und auf dem Boden steht
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;

        bool isWalking =
            Time.timeScale > 0f &&
            moveDirection.sqrMagnitude > 0.01f &&
            velocity.sqrMagnitude > 0.25f &&
            IsGrounded();

        float targetVolume = isWalking ? walkingVolume : 0f;

        footstepSource.volume = Mathf.MoveTowards(
            footstepSource.volume,
            targetVolume,
            footstepFadeSpeed * walkingVolume * Time.unscaledDeltaTime
        );

        if (isWalking && !footstepSource.isPlaying)
        {
            // UnPause setzt an der gleichen Stelle fort,
            // Play startet beim allerersten Mal von vorne
            if (footstepSource.time > 0f)
                footstepSource.UnPause();
            else
                footstepSource.Play();
        }
        else if (!isWalking && footstepSource.isPlaying && footstepSource.volume <= 0.001f)
        {
            footstepSource.Pause();
        }
    }

    [Header("Kanten")]
    // Wie weit unter der Kapsel noch Boden gesucht wird, wenn man eine Kante runterläuft
    public float groundSnapDistance = 0.35f;
    // Wie schnell der Spieler dann nach unten an den Boden gezogen wird
    public float groundSnapSpeed = 4f;

    private bool wasGroundedLastStep;

    private CapsuleCollider groundCheckCapsule;

    private bool IsGrounded()
    {
        return GroundCheck(0.2f);
    }

    private bool GroundCheck(float distance)
    {
        // Prüft direkt unter der Kapsel (nicht unter dem Pivot),
        // denn die Kapsel beginnt erst ein Stück über dem Pivot.
        if (groundCheckCapsule == null)
            groundCheckCapsule = GetComponent<CapsuleCollider>();

        if (groundCheckCapsule == null)
            return false;

        Bounds b = groundCheckCapsule.bounds;
        float radius = b.extents.x * 0.9f;
        Vector3 origin = new Vector3(b.center.x, b.min.y + radius + 0.05f, b.center.z);

        // SphereCast statt Raycast: findet den Boden auch an Kanten (Bordstein, Stufen)
        return Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out _,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore
        );
    }

    private void OnDisable()
    {
        // Steuerung aus (z. B. Sitzen, Zwischensequenz) -> Schritte sofort stumm
        if (footstepSource != null)
        {
            footstepSource.volume = 0f;
            footstepSource.Pause();
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
