using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// =====================================================
// TANKSTELLE - KASSIERER IM HINTERZIMMER (JUMPSCARE)
// Am Anfang steht der Kassierer NICHT an der Theke, sondern wartet
// im Hinterzimmer. Sobald der Spieler eine Tür zum Hinterzimmer öffnet:
//
//   von AUSSEN (Hintertür):
//     Jumpscare -> "What are you trying to do?" -> er knallt die Tür zu
//     -> danach steht er an der Theke
//   von INNEN (Tür im Shop):
//     Jumpscare -> "What are you trying to do?" -> er geht durch die Tür,
//     schließt sie hinter sich und läuft zur Theke
//
// Während der ganzen Sequenz kann der Spieler sich nicht bewegen.
// Danach sind beide Hinterzimmer-Türen abgeschlossen. Passiert nur einmal.
// =====================================================
[DefaultExecutionOrder(-1000)] // Update läuft vor der Kamera-Steuerung (für das Wackeln)
public class CashierBackroomJumpscare : MonoBehaviour
{
    [Header("Referenzen")]
    public GasStationIntro intro;
    [Tooltip("Interact_Cashier - wird erst nach der Sequenz eingeschaltet")]
    public CashierInteractable cashierInteract;
    [Tooltip("Hintertür von außen (Door_Back)")]
    public DoorInteraction backDoor;
    [Tooltip("Tür vom Shop ins Hinterzimmer (Door_Backroom)")]
    public DoorInteraction backroomDoor;
    public Transform player;
    [Tooltip("Die Kamera, die wackeln soll (Main Camera des Spielers)")]
    public Transform playerCamera;

    [Header("Kassierer")]
    public Transform cashier;
    public Animator cashierAnimator;
    [Tooltip("Hier wartet er am Anfang (im Hinterzimmer, nicht sichtbar)")]
    public Transform hiddenPoint;
    [Tooltip("Hier steht er, wenn man die Hintertür von AUSSEN öffnet")]
    public Transform scarePointOutside;
    [Tooltip("Hier steht er, wenn man die Hinterzimmer-Tür vom SHOP aus öffnet")]
    public Transform scarePointInside;
    [Tooltip("Sein Platz an der Theke (Position + Blickrichtung)")]
    public Transform counterPoint;
    [Tooltip("Laufweg von der Tür bis zur Theke (nur bei 'von innen'), in Reihenfolge")]
    public Transform[] walkPath;
    [Tooltip("Nach diesem Wegpunkt (Index) schließt er die Tür hinter sich")]
    public int closeDoorAfterPoint = 1;
    public float walkSpeed = 1.3f;
    [Tooltip("So viel Abstand hält er beim Vorbeigehen zum Spieler")]
    public float passDistance = 0.7f;

    [Header("Text")]
    [Tooltip("Was er sagt, wenn man von AUSSEN rein will")]
    public string[] linesOutside = { "What are you trying to do?" };
    [Tooltip("Was er sagt, wenn man vom SHOP aus rein will")]
    public string[] linesInside = { "What are you trying to do?" };
    [Tooltip("Satz des Spielers an den Türen, wenn sie danach abgeschlossen sind (leer = nichts)")]
    public string lockedLine = "It's locked.";
    public float lockedLineCooldown = 3f;

    [Header("Sound")]
    public AudioSource audioSource;
    [Tooltip("HIER den Jumpscare-Sound reinziehen")]
    public AudioClip jumpscareSound;
    [Range(0f, 1f)] public float jumpscareVolume = 1f;
    [Tooltip("Optional: Sound, wenn er die Hintertür zuknallt")]
    public AudioClip doorSlamSound;
    [Range(0f, 1f)] public float doorSlamVolume = 1f;

    [Header("Timing")]
    [Tooltip("Sekunden nach dem Türöffnen, bis Sound + Wackeln starten")]
    public float scareDelay = 0.12f;
    [Tooltip("Pause nach dem Schreck, bevor er redet")]
    public float pauseBeforeTalking = 0.8f;

    [Header("Kamera-Wackeln (Jumpscare)")]
    public float shakeDuration = 0.7f;
    public float shakePosition = 0.12f;
    public float shakeRotation = 6f;
    public float shakeFrequency = 30f;
    [Header("Kamera-Wackeln (Tür knallt)")]
    public float slamShakeDuration = 0.3f;
    public float slamShakeStrength = 0.4f;

    [Header("Spieler")]
    [Tooltip("Spieler darf sich umschauen, während der Kassierer zur Theke läuft (bewegen geht trotzdem nicht)")]
    public bool allowLookWhileWalking = true;

    [Header("Danach")]
    public UnityEvent onSequenceFinished;

    private bool done, running;
    private float nextLockedLine;
    private Collider cashierCollider;
    private readonly List<Behaviour> frozen = new List<Behaviour>();

    // Kamera-Wackeln
    private float shakeTime = -1f, shakeLength, shakeScale = 1f;
    private Vector3 appliedPos;
    private Quaternion appliedRot = Quaternion.identity;
    private bool shakeApplied;

    void OnEnable()
    {
        if (backDoor != null) { backDoor.OnOpened += OpenedOutside; backDoor.OnLockedAttempt += HandleLocked; }
        if (backroomDoor != null) { backroomDoor.OnOpened += OpenedInside; backroomDoor.OnLockedAttempt += HandleLocked; }
    }

    void OnDisable()
    {
        if (backDoor != null) { backDoor.OnOpened -= OpenedOutside; backDoor.OnLockedAttempt -= HandleLocked; }
        if (backroomDoor != null) { backroomDoor.OnOpened -= OpenedInside; backroomDoor.OnLockedAttempt -= HandleLocked; }
        RemoveShake();
    }

    void Start()
    {
        if (cashier != null) cashierCollider = cashier.GetComponent<Collider>();
        if (cashierAnimator == null && cashier != null) cashierAnimator = cashier.GetComponent<Animator>();

        // Anfang: Kassierer versteckt, an der Theke kann man niemanden ansprechen
        Place(hiddenPoint, false);
        if (cashierInteract != null) cashierInteract.gameObject.SetActive(false);

        // Türen lassen sich öffnen -> lösen die Sequenz aus
        if (backDoor != null) backDoor.IsLocked = false;
        if (backroomDoor != null) backroomDoor.IsLocked = false;
    }

    // =================================
    // AUSLÖSER
    // =================================
    private void OpenedOutside() => Trigger(true);
    private void OpenedInside() => Trigger(false);

    private void Trigger(bool fromOutside)
    {
        if (done) return;
        done = true;
        StartCoroutine(Sequence(fromOutside));
    }

    private void HandleLocked()
    {
        if (running || string.IsNullOrEmpty(lockedLine) || intro == null || Time.time < nextLockedLine) return;
        nextLockedLine = Time.time + lockedLineCooldown;
        StartCoroutine(intro.PlayerSay(lockedLine));
    }

    // =================================
    // ABLAUF
    // =================================
    private IEnumerator Sequence(bool fromOutside)
    {
        running = true;
        Freeze(true);

        // 1) Kassierer steht direkt hinter der Tür und schaut den Spieler an
        Place(fromOutside ? scarePointOutside : scarePointInside, true);

        // 2) Schreck: Sound + starkes Wackeln
        if (scareDelay > 0f) yield return new WaitForSeconds(scareDelay);
        if (audioSource != null && jumpscareSound != null) audioSource.PlayOneShot(jumpscareSound, jumpscareVolume);
        StartShake(shakeDuration, 1f);
        yield return new WaitForSeconds(pauseBeforeTalking);

        // 3) "What are you trying to do?"
        foreach (var line in fromOutside ? linesOutside : linesInside)
        {
            if (string.IsNullOrEmpty(line) || intro == null) continue;
            if (cashierAnimator != null) cashierAnimator.SetTrigger("Talk");
            yield return intro.NpcSay(line, cashierInteract != null ? cashierInteract.cashierTalking : null);
        }

        if (fromOutside)
        {
            // 4a) Tür zuknallen, danach steht er an der Theke
            yield return new WaitForSeconds(0.15f);
            if (backDoor != null && backDoor.IsOpen) backDoor.Interact();
            if (doorSlamSound != null && backDoor != null)
                AudioSource.PlayClipAtPoint(doorSlamSound, backDoor.transform.position, doorSlamVolume);
            StartShake(slamShakeDuration, slamShakeStrength);
            yield return new WaitForSeconds(0.6f);
            Place(counterPoint, false);
        }
        else
        {
            // 4b) durch die Tür, Tür schließen, zur Theke laufen
            if (cashierCollider != null) cashierCollider.enabled = false;
            if (allowLookWhileWalking) SetLook(true);

            for (int i = 0; i < walkPath.Length; i++)
            {
                if (walkPath[i] == null) continue;
                yield return WalkTo(walkPath[i].position);
                if (i == closeDoorAfterPoint && backroomDoor != null && backroomDoor.IsOpen)
                {
                    yield return TurnTo(backroomDoor.transform.position - cashier.position, 0.25f);
                    backroomDoor.Interact();
                    yield return new WaitForSeconds(0.4f);
                }
            }
            if (counterPoint != null)
            {
                yield return WalkTo(counterPoint.position);
                yield return TurnTo(counterPoint.forward, 0.3f);
            }
            if (cashierCollider != null) cashierCollider.enabled = true;
        }

        // 5) Aufräumen: Türen zu, Kassierer ansprechbar, Spieler frei
        if (backDoor != null) backDoor.IsLocked = true;
        if (backroomDoor != null) backroomDoor.IsLocked = true;
        if (cashierInteract != null) cashierInteract.gameObject.SetActive(true);
        Freeze(false);
        running = false;
        onSequenceFinished?.Invoke();
    }

    // =================================
    // KASSIERER BEWEGEN
    // =================================
    private void Place(Transform point, bool facePlayer)
    {
        if (cashier == null || point == null) return;
        cashier.position = point.position;
        Quaternion rot = point.rotation;
        if (facePlayer && player != null)
        {
            Vector3 d = player.position - point.position; d.y = 0f;
            if (d.sqrMagnitude > 0.001f) rot = Quaternion.LookRotation(d);
        }
        cashier.rotation = rot;
    }

    private IEnumerator WalkTo(Vector3 target)
    {
        foreach (var p in AvoidPlayer(cashier.position, target))
            yield return WalkStraight(p);
    }

    private IEnumerator WalkStraight(Vector3 target)
    {
        float baseY = target.y;
        float t = 0f;
        while (true)
        {
            Vector3 pos = cashier.position; pos.y = baseY;
            Vector3 d = target - pos; d.y = 0f;
            if (d.magnitude < 0.03f) break;
            Vector3 step = d.normalized * Mathf.Min(walkSpeed * Time.deltaTime, d.magnitude);
            pos += step;
            t += Time.deltaTime;
            pos.y = baseY + Mathf.Abs(Mathf.Sin(t * 7f)) * 0.03f;   // leichtes Wippen beim Gehen
            cashier.position = pos;
            cashier.rotation = Quaternion.RotateTowards(cashier.rotation, Quaternion.LookRotation(d), 540f * Time.deltaTime);
            yield return null;
        }
        Vector3 end = target; cashier.position = end;
    }

    private IEnumerator TurnTo(Vector3 dir, float duration)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) yield break;
        Quaternion from = cashier.rotation, to = Quaternion.LookRotation(dir);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            cashier.rotation = Quaternion.Slerp(from, to, t / duration);
            yield return null;
        }
        cashier.rotation = to;
    }

    // Kleiner Bogen um den Spieler, wenn er im Weg steht
    private List<Vector3> AvoidPlayer(Vector3 a, Vector3 b)
    {
        var pts = new List<Vector3>();
        if (player != null)
        {
            Vector3 p = player.position; p.y = a.y;
            Vector3 ab = b - a; ab.y = 0f;
            float len = ab.magnitude;
            if (len > 0.01f)
            {
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / (len * len));
                Vector3 c = a + ab * t;
                float dist = Vector3.Distance(c, p);
                if (dist < passDistance && t > 0.05f && t < 0.95f)
                {
                    Vector3 side = Vector3.Cross(Vector3.up, ab.normalized);
                    if (Vector3.Dot(c - p, side) < 0f) side = -side;
                    foreach (var s in new[] { side, -side })
                    {
                        Vector3 detour = p + s * passDistance; detour.y = a.y;
                        if (!Physics.CheckCapsule(detour + Vector3.up * 0.5f, detour + Vector3.up * 1.6f, 0.25f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            pts.Add(detour);
                            break;
                        }
                    }
                }
            }
        }
        // Liegt der Wegpunkt selbst zu nah am Spieler, etwas weiter weg ausweichen
        if (player != null)
        {
            Vector3 p = player.position; p.y = b.y;
            Vector3 away = b - p; away.y = 0f;
            if (away.magnitude < passDistance)
            {
                Vector3 dir = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.Cross(Vector3.up, (b - a).normalized);
                foreach (var s in new[] { dir, Vector3.Cross(Vector3.up, dir), -Vector3.Cross(Vector3.up, dir) })
                {
                    Vector3 alt = p + s * passDistance; alt.y = b.y;
                    if (!Physics.CheckCapsule(alt + Vector3.up * 0.5f, alt + Vector3.up * 1.6f, 0.25f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        b = alt;
                        break;
                    }
                }
            }
        }
        pts.Add(b);
        return pts;
    }

    // =================================
    // SPIELER EINFRIEREN
    // =================================
    private void Freeze(bool freeze)
    {
        if (freeze)
        {
            frozen.Clear();
            if (player == null) return;
            foreach (var b in player.GetComponentsInChildren<Behaviour>())
            {
                if (b is PlayerController || b is CameraController || b is PlayerInteractor || b is PickupItem)
                {
                    if (b is PlayerInteractor pi && pi.promptText != null) pi.promptText.gameObject.SetActive(false);
                    if (b.enabled) { b.enabled = false; frozen.Add(b); }
                }
            }
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
        }
        else
        {
            foreach (var b in frozen) if (b != null) b.enabled = true;
            frozen.Clear();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void SetLook(bool on)
    {
        foreach (var b in frozen) if (b is CameraController) b.enabled = on;
    }

    // =================================
    // KAMERA-WACKELN
    // Wird in LateUpdate (nach der Maus-Steuerung) draufgerechnet
    // und im nächsten Update wieder abgezogen -> verstellt die Kamera nicht dauerhaft.
    // =================================
    private void StartShake(float duration, float scale)
    {
        shakeLength = duration; shakeScale = scale; shakeTime = 0f;
    }

    void Update()
    {
        RemoveShake();
    }

    void LateUpdate()
    {
        if (shakeTime < 0f || playerCamera == null) return;

        shakeTime += Time.deltaTime;
        float t = shakeTime / Mathf.Max(0.01f, shakeLength);
        if (t >= 1f) { shakeTime = -1f; return; }

        float strength = (1f - t) * (1f - t) * shakeScale;
        float s = Time.time * shakeFrequency;

        appliedPos = new Vector3(
            (Mathf.PerlinNoise(s, 0f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(0f, s) - 0.5f) * 2f,
            0f) * shakePosition * strength;

        appliedRot = Quaternion.Euler(
            (Mathf.PerlinNoise(s, 10f) - 0.5f) * 2f * shakeRotation * strength,
            (Mathf.PerlinNoise(20f, s) - 0.5f) * 2f * shakeRotation * strength,
            (Mathf.PerlinNoise(s, 30f) - 0.5f) * 2f * shakeRotation * strength);

        playerCamera.localPosition += appliedPos;
        playerCamera.localRotation *= appliedRot;
        shakeApplied = true;
    }

    private void RemoveShake()
    {
        if (!shakeApplied || playerCamera == null) return;
        playerCamera.localPosition -= appliedPos;
        playerCamera.localRotation *= Quaternion.Inverse(appliedRot);
        shakeApplied = false;
    }
}
