using System.Collections;
using UnityEngine;

// =====================================================
// STORY 2 - ENDE: DER KILLER SCHUBST DEN SPIELER VOM TOWER
// Wird von WatchtowerStory "scharf geschaltet" (Arm), nachdem die
// Kabine untersucht wurde. Betritt der Spieler dann das obere
// Treppenpodest (pushZone):
//   1. Spieler eingefroren, Geräusch hinter ihm
//   2. Kamera dreht sich um -> der Killer steht direkt hinter ihm
//   3. Killer packt zu und stößt ihn übers Geländer
//   4. Sturz (Kamera fällt rückwärts, Blick auf den Killer oben)
//   5. Aufprall -> Schwarzbild -> Story 2 abgeschlossen -> Hauptmenü
// =====================================================
public class TowerPushSequence : MonoBehaviour
{
    [Header("Referenzen")]
    public WatchtowerStory story;
    public GameObject player;
    public Transform playerCamera;
    [Tooltip("Bereich auf dem oberen Treppenpodest (Trigger-BoxCollider)")]
    public BoxCollider pushZone;

    [Header("Killer")]
    public GameObject killer;
    public Animator killerAnimator;
    [Tooltip("Hier steht der Killer, wenn der Spieler sich umdreht (hinter dem Spieler)")]
    public Transform killerPoint;
    [Tooltip("So dicht steht der Killer hinter dem Spieler (Meter)")]
    public float killerDistance = 0.95f;
    public float killerEyeHeight = 1.7f;

    [Header("Sound")]
    public AudioSource audioSource;
    [Tooltip("Geräusch hinter dem Spieler (z. B. knarrende Dielen)")]
    public AudioClip creakSound;
    [Tooltip("Schreck-Sound, wenn man den Killer sieht")]
    public AudioClip stingSound;
    [Tooltip("Stoß")]
    public AudioClip pushSound;
    [Tooltip("Schrei beim Fallen")]
    public AudioClip fallScream;
    [Tooltip("Aufprall unten")]
    public AudioClip impactSound;

    [Header("Timing")]
    public float turnDelay = 0.8f;
    public float turnDuration = 0.45f;
    public float stareDuration = 0.9f;

    [Header("Sturz")]
    public float pushSpeed = 2.6f;
    public float pushUpSpeed = 2.6f;
    public float gravity = 9.81f;
    public float holdBlackSeconds = 3f;

    [Header("Danach")]
    public string menuScene = "MenueScene";

    public bool IsRunning => running;

    private bool armed, running;

    void Start()
    {
        if (killer != null) killer.SetActive(false);
    }

    public void Arm() => armed = true;

    void Update()
    {
        if (!armed || running || player == null || pushZone == null) return;
        // Pivot des Spielers liegt unter dem Boden -> etwas höher prüfen
        Vector3 p = player.transform.position + Vector3.up * 1.0f;
        if (pushZone.bounds.Contains(p))
        {
            running = true;
            StartCoroutine(Sequence());
        }
    }

    private IEnumerator Sequence()
    {
        Freeze();
        story?.HideTask();

        // 1) Geräusch hinter dem Spieler
        Play(creakSound);
        yield return new WaitForSeconds(turnDelay);

        // 2) Killer steht hinter ihm, Kamera dreht sich um
        // dicht hinter den Spieler stellen (Richtung KillerPoint, höchstens killerDistance entfernt)
        Vector3 pFlat = player.transform.position; pFlat.y = killerPoint.position.y;
        Vector3 toPoint = killerPoint.position - pFlat;
        Vector3 killerPos = pFlat + toPoint.normalized * Mathf.Min(toPoint.magnitude, killerDistance);
        Vector3 toPlayer = player.transform.position - killerPos; toPlayer.y = 0f;
        killer.transform.SetPositionAndRotation(killerPos, Quaternion.LookRotation(toPlayer.normalized));
        killer.SetActive(true);

        Vector3 eye = killerPos + Vector3.up * killerEyeHeight;
        Quaternion from = playerCamera.rotation;
        Quaternion to = Quaternion.LookRotation(eye - playerCamera.position);
        bool stung = false;
        for (float t = 0f; t < turnDuration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / turnDuration);
            playerCamera.rotation = Quaternion.Slerp(from, to, k);
            if (!stung && k > 0.6f) { stung = true; Play(stingSound); }
            yield return null;
        }
        playerCamera.rotation = to;
        yield return new WaitForSeconds(stareDuration);

        // 3) Packen + Stoß
        if (killerAnimator != null) killerAnimator.SetTrigger("Grab");
        Vector3 pushDir = playerCamera.position - eye; pushDir.y = 0f; pushDir.Normalize();
        Vector3 lungeFrom = killer.transform.position;
        Vector3 lungeTo = lungeFrom + pushDir * 0.5f;
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            killer.transform.position = Vector3.Lerp(lungeFrom, lungeTo, t / 0.25f);
            yield return null;
        }
        Play(pushSound);

        // 4) Sturz: Kamera löst sich vom Spieler und fällt rückwärts
        playerCamera.SetParent(null, true);
        Vector3 pos = playerCamera.position;
        float groundY = GroundY(pos + pushDir * 4f, pos.y);
        Vector3 vel = pushDir * pushSpeed + Vector3.up * pushUpSpeed;
        Play(fallScream);
        float roll = 0f;
        while (pos.y > groundY + 0.35f)
        {
            vel += Vector3.down * gravity * Time.deltaTime;
            pos += vel * Time.deltaTime;
            playerCamera.position = pos;

            // Killer schaut hinterher, Kamera schaut zu ihm hoch
            Vector3 look = pos - killer.transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.01f) killer.transform.rotation = Quaternion.Slerp(killer.transform.rotation, Quaternion.LookRotation(look), 5f * Time.deltaTime);
            roll += 40f * Time.deltaTime;
            playerCamera.rotation = Quaternion.LookRotation(killer.transform.position + Vector3.up * killerEyeHeight - pos) * Quaternion.Euler(0f, 0f, roll);
            yield return null;
        }

        // 5) Aufprall -> sofort schwarz, Umgebungsgeräusche aus
        if (story != null && story.blackScreen != null)
        {
            story.blackScreen.gameObject.SetActive(true);
            story.blackScreen.alpha = 1f;
        }
        foreach (var src in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            if (src != audioSource) src.Stop();
        Play(impactSound);
        yield return new WaitForSeconds(holdBlackSeconds);

        StoryProgress.CompleteStory2();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneLoader.Load(menuScene);
    }

    private void Play(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    private void Freeze()
    {
        foreach (var b in player.GetComponentsInChildren<Behaviour>())
            if (b is PlayerController || b is CameraController || b is PlayerInteractor || b is PickupItem)
            {
                if (b is PlayerInteractor pi && pi.promptText != null) pi.promptText.gameObject.SetActive(false);
                b.enabled = false;
            }
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
    }

    // Boden an der Stelle, wo der Spieler aufkommt (Tower-Teile weiter oben ignorieren)
    private float GroundY(Vector3 around, float fromY)
    {
        float y = 0f, best = float.MaxValue;
        foreach (var h in Physics.RaycastAll(new Vector3(around.x, fromY, around.z), Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(player.transform)) continue;
            if (h.point.y > fromY - 3f) continue;
            if (h.distance < best) { best = h.distance; y = h.point.y; }
        }
        return y;
    }
}
