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
    // = Standard, z. B. Asphalt, Beton, Fliesen
    public AudioClip walkingClip;

    [Range(0f, 1f)]
    public float walkingVolume = 0.6f;

    // Wie schnell der Sound ein-/ausgeblendet wird (verhindert Knacken)
    public float footstepFadeSpeed = 8f;

    [Header("Schritte je nach Boden")]
    [Tooltip("Lauf-Sound auf Holz (leer = Standard-Sound)")]
    public AudioClip woodClip;
    [Range(0f, 2f)] public float woodVolume = 1f;
    [Tooltip("Lauf-Sound auf Gras / Waldboden (leer = Standard-Sound)")]
    public AudioClip grassClip;
    [Range(0f, 2f)] public float grassVolume = 1f;
    [Tooltip("Boden zählt als Holz, wenn Material- oder Objektname eines dieser Wörter enthält")]
    public string[] woodKeywords = { "wood", "holz", "plank", "parkett" };
    [Tooltip("Boden zählt als Gras, wenn Material- oder Objektname eines dieser Wörter enthält")]
    public string[] grassKeywords = { "grass", "gras", "forest", "soil", "moss", "leaf", "lawn", "wiese" };
    [Tooltip("Wie oft (Sekunden) der Boden geprüft wird")]
    public float surfaceCheckInterval = 0.15f;

    private AudioSource footstepSource;
    private float surfaceTimer;
    private float surfaceVolume = 1f;


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

        float horizontal = InputBindings.GetHorizontal();
        float vertical = InputBindings.GetVertical();

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

        if (InputBindings.GetDown(GameAction.Flashlight))
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
        if (walkingClip == null && woodClip == null && grassClip == null)
            return;

        // Eigene AudioSource nur für die Schritte,
        // damit sich nichts mit anderen Sounds (z. B. Stimme) überschneidet
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.clip = walkingClip != null ? walkingClip : (grassClip != null ? grassClip : woodClip);
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

        // Boden prüfen und passenden Sound wählen
        if (isWalking)
        {
            surfaceTimer -= Time.deltaTime;
            if (surfaceTimer <= 0f)
            {
                surfaceTimer = surfaceCheckInterval;
                ChooseFootstepClip();
            }
        }

        float targetVolume = isWalking ? walkingVolume * surfaceVolume : 0f;

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

    // Welcher Boden ist unter dem Spieler? -> Holz, Gras oder Standard
    // Zum Nachschauen: welcher Boden zuletzt erkannt wurde und welcher Sound läuft
    public string CurrentSurface { get; private set; } = "-";
    public AudioClip CurrentFootstepClip => footstepSource != null ? footstepSource.clip : null;

    // Boden sofort neu prüfen (sonst alle surfaceCheckInterval Sekunden beim Laufen)
    public void RefreshFootstepSurface() { if (footstepSource != null) ChooseFootstepClip(); }

    private void ChooseFootstepClip()
    {
        AudioClip clip = walkingClip;
        float vol = 1f;

        string surface = GetSurfaceName();
        if (surface != null)
        {
            if (woodClip != null && ContainsAny(surface, woodKeywords)) { clip = woodClip; vol = woodVolume; }
            else if (grassClip != null && ContainsAny(surface, grassKeywords)) { clip = grassClip; vol = grassVolume; }
        }
        if (clip == null) clip = footstepSource.clip;
        surfaceVolume = vol;
        CurrentSurface = surface ?? "-";

        if (clip != footstepSource.clip)
        {
            bool wasPlaying = footstepSource.isPlaying;
            footstepSource.clip = clip;
            // nicht jedes Mal am Anfang starten, klingt sonst gleich
            footstepSource.time = Random.Range(0f, clip.length * 0.5f);
            if (wasPlaying) footstepSource.Play();
        }
    }

    // Materialname der Fläche direkt unter dem Spieler (sonst Objektname), klein geschrieben
    private string GetSurfaceName()
    {
        if (groundCheckCapsule == null)
            groundCheckCapsule = GetComponent<CapsuleCollider>();
        Vector3 origin = groundCheckCapsule != null ? groundCheckCapsule.bounds.center : transform.position + Vector3.up;
        float dist = groundCheckCapsule != null ? groundCheckCapsule.bounds.extents.y + 0.8f : 2.5f;

        RaycastHit best = default;
        float bestDist = float.MaxValue;
        foreach (var h in Physics.RaycastAll(origin, Vector3.down, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.distance < bestDist) { bestDist = h.distance; best = h; }
        }
        if (best.collider == null) return null;

        // eigene Markierung hat Vorrang
        var marker = best.collider.GetComponentInParent<FootstepSurface>();
        if (marker != null) return marker.surface.ToString().ToLowerInvariant();

        if (best.collider is TerrainCollider) return "grass terrain";

        string name = best.collider.name;
        var r = best.collider.GetComponent<Renderer>();
        if (r != null)
        {
            Material m = r.sharedMaterial;
            // Mesh mit mehreren Materialien: das Material der getroffenen Fläche nehmen
            var mc = best.collider as MeshCollider;
            var mats = r.sharedMaterials;
            if (mc != null && mc.sharedMesh != null && mats.Length > 1 && best.triangleIndex >= 0)
            {
                Mesh mesh = mc.sharedMesh;
                int index = best.triangleIndex * 3;
                for (int i = 0; i < mesh.subMeshCount && i < mats.Length; i++)
                {
                    var sm = mesh.GetSubMesh(i);
                    if (sm.topology == MeshTopology.Triangles && index >= sm.indexStart && index < sm.indexStart + sm.indexCount)
                    {
                        m = mats[i];
                        break;
                    }
                }
            }
            // Das Material entscheidet (ein Objekt "grass_soil" kann auch einen Betonweg enthalten),
            // der Objektname zählt nur, wenn es kein Material gibt
            if (m != null) name = m.name;
        }
        return name.ToLowerInvariant();
    }

    private static bool ContainsAny(string text, string[] words)
    {
        if (words == null) return false;
        foreach (var w in words)
            if (!string.IsNullOrEmpty(w) && text.Contains(w.ToLowerInvariant())) return true;
        return false;
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
