using UnityEngine;

// =====================================================
// ATMEN HINTER EINER ABGESCHLOSSENEN TÜR (z. B. WC)
// Die Tür ist abgeschlossen. Kommt der Spieler nah an die Tür,
// hört er leises Atmen (wird lauter, je näher er ist).
// Rüttelt er an der Tür, hält die Person kurz den Atem an.
// Auf ein Objekt HINTER der Tür legen (Richtung kommt daher).
// =====================================================
[RequireComponent(typeof(AudioSource))]
public class BreathingBehindDoor : MonoBehaviour
{
    [Header("Referenzen")]
    public DoorInteraction door;
    public Transform player;
    public GasStationIntro intro;

    [Header("Sound")]
    [Tooltip("HIER den Atem-Sound reinziehen (läuft in Schleife)")]
    public AudioClip breathingSound;
    [Range(0f, 1f)] public float maxVolume = 0.5f;
    [Tooltip("Ab dieser Entfernung zur Tür hört man das Atmen (Meter)")]
    public float hearRadius = 2.5f;
    public float fadeSpeed = 1.5f;

    [Header("Abgeschlossen")]
    public bool lockDoor = true;
    [Tooltip("Satz des Spielers, wenn er an der Tür rüttelt (leer = nichts)")]
    public string lockedLine = "Locked. ...Is someone in there?";
    public float lockedLineCooldown = 4f;
    [Tooltip("Atmen hört kurz auf, wenn man an der Tür rüttelt")]
    public bool holdBreathOnHandle = true;
    public float holdBreathSeconds = 4f;

    private AudioSource src;
    private Vector3 doorCenter;
    private float volume, silentUntil, nextLine;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 1f;
        // Lautstärke steuert das Script über die Entfernung zur TÜR,
        // Unity soll nur die Richtung (links/rechts) machen
        src.rolloffMode = AudioRolloffMode.Custom;
        src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 1f, 1f));
        src.maxDistance = 50f;
        src.volume = 0f;
    }

    void Start()
    {
        if (door != null)
        {
            if (lockDoor) door.IsLocked = true;
            var r = door.GetComponent<Renderer>();
            doorCenter = r != null ? r.bounds.center : door.transform.position;
        }
        else doorCenter = transform.position;
    }

    void OnEnable() { if (door != null) door.OnLockedAttempt += HandleLocked; }
    void OnDisable() { if (door != null) door.OnLockedAttempt -= HandleLocked; }

    void Update()
    {
        if (breathingSound != null && src.clip != breathingSound) { src.clip = breathingSound; src.Play(); }
        if (src.clip == null || player == null) return;
        if (!src.isPlaying) src.Play();

        Vector3 a = player.position, b = doorCenter; a.y = 0f; b.y = 0f;
        float d = Vector3.Distance(a, b);
        float target = (Time.time >= silentUntil) ? maxVolume * Mathf.Clamp01(1f - d / hearRadius) : 0f;
        volume = Mathf.MoveTowards(volume, target, fadeSpeed * Time.deltaTime);
        src.volume = volume;
    }

    private void HandleLocked()
    {
        if (holdBreathOnHandle)
        {
            silentUntil = Time.time + holdBreathSeconds;
            volume = 0f;            // sofort still - wirkt unheimlicher als ausblenden
            src.volume = 0f;
        }
        if (string.IsNullOrEmpty(lockedLine) || intro == null || Time.time < nextLine) return;
        nextLine = Time.time + lockedLineCooldown;
        StartCoroutine(intro.PlayerSay(lockedLine));
    }
}
