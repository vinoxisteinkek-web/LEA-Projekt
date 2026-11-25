using System.Collections;
using UnityEngine;

/// <summary>
/// Ending 3 (Killer Door):
/// Die Schranktür geht auf und der Stalker greift aus dem Schrank nach dem Spieler.
/// Wird von StoryManagerAfterMarket gestartet. Danach läuft das normale Ending
/// (schwarzer Bildschirm, Schrei, Texte) weiter.
/// </summary>
public class KillerDoorStalkerSequence : MonoBehaviour
{
    // ==================================================
    // TÜR
    // ==================================================

    [Header("Tür")]
    [SerializeField] private Transform door;

    // Dreht wie DoorInteraction um die lokale Z-Achse.
    // Negativer Wert = Tür öffnet in die andere Richtung.
    [SerializeField] private float doorOpenAngle = 90f;
    [SerializeField] private float doorOpenTime = 0.35f;

    [SerializeField] private AudioSource doorAudioSource;
    [SerializeField] private AudioClip doorSound;


    // ==================================================
    // STALKER
    // ==================================================

    [Header("Stalker")]
    [SerializeField] private Transform stalker;
    [SerializeField] private Animator stalkerAnimator;

    // Kopf-Bone des Stalkers: darauf schaut die Kamera
    [SerializeField] private Transform stalkerHead;
    [SerializeField] private float lookHeightOffset = 0.14f;

    [SerializeField] private string grabTrigger = "Grab";

    // Tür ist offen, der Stalker steht kurz nur da
    [SerializeField] private float revealPause = 0.6f;

    // Dauer des Zugreifens (passt zur Grab-Animation)
    [SerializeField] private float lungeTime = 0.3f;

    // Abstand zwischen Stalker und Kamera am Ende des Zugreifens.
    // Bei ca. 1.7 sind die Hände direkt vor der Kamera.
    [SerializeField] private float stopDistance = 1.7f;

    // Kurzer Moment mit den Händen vor der Kamera, bevor es schwarz wird
    [SerializeField] private float blackoutDelay = 0.06f;


    // ==================================================
    // SPIELER
    // ==================================================

    [Header("Spieler")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Camera playerCamera;

    // Punkt vor der Tür (blaue Z-Achse zeigt in den Schrank).
    // Der Spieler wird beim Öffnen der Tür in diesen Bereich geschoben,
    // damit der Stalker ihn durch die Türöffnung erreichen kann.
    [SerializeField] private Transform victimPoint;
    [SerializeField] private float victimSideTolerance = 0.25f;
    [SerializeField] private float victimNearTolerance = 0.3f;
    [SerializeField] private float victimFarTolerance = 0.45f;
    [SerializeField] private float playerMoveSpeed = 4f;

    // Wie schnell sich die Kamera zum Stalker dreht
    [SerializeField] private float cameraTurnSpeed = 12f;

    private bool isPlaying = false;


    // ==================================================
    // SEQUENZ
    // ==================================================

    public IEnumerator Play()
    {
        if (isPlaying)
            yield break;

        isPlaying = true;

        FindMissingReferences();

        Transform cam = playerCamera != null ? playerCamera.transform : null;

        // Spieler kann sich nicht mehr bewegen oder umsehen
        Transform player = FreezePlayer();

        Vector3 playerStart = player != null ? player.position : Vector3.zero;
        Vector3 playerTarget = GetPlayerTarget(playerStart, cam);

        float playerMoveTime = Mathf.Clamp(
            Vector3.Distance(playerStart, playerTarget) / Mathf.Max(0.1f, playerMoveSpeed),
            0.15f,
            0.5f
        );

        Quaternion doorClosed = Quaternion.identity;
        Quaternion doorOpen = Quaternion.identity;

        if (door != null)
        {
            doorClosed = door.localRotation;
            doorOpen = doorClosed * Quaternion.Euler(0f, 0f, doorOpenAngle);
        }

        if (doorAudioSource != null && doorSound != null)
        {
            doorAudioSource.PlayOneShot(doorSound);
        }


        // --------------------------------------------------
        // 1) Tür geht auf, Kamera dreht sich zum Stalker
        // --------------------------------------------------

        float openTime = Mathf.Max(0.01f, doorOpenTime);
        float duration = Mathf.Max(openTime, playerMoveTime);
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            if (door != null)
            {
                // Schnell aufschwingen, am Ende abbremsen
                float k = Mathf.Clamp01(time / openTime);
                k = 1f - Mathf.Pow(1f - k, 3f);

                door.localRotation = Quaternion.Slerp(doorClosed, doorOpen, k);
            }

            if (player != null)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / playerMoveTime));

                player.position = Vector3.Lerp(playerStart, playerTarget, k);
            }

            LookAtStalker(cam);

            yield return null;
        }

        if (door != null)
        {
            door.localRotation = doorOpen;
        }


        // --------------------------------------------------
        // 2) Der Stalker steht im Schrank und starrt
        // --------------------------------------------------

        time = 0f;

        while (time < revealPause)
        {
            time += Time.deltaTime;

            LookAtStalker(cam);

            yield return null;
        }


        // --------------------------------------------------
        // 3) Der Stalker greift nach dem Spieler
        // --------------------------------------------------

        if (stalkerAnimator != null)
        {
            stalkerAnimator.SetTrigger(grabTrigger);
        }

        float grabTime = Mathf.Max(0.01f, lungeTime);

        Vector3 stalkerStart = Vector3.zero;
        Vector3 stalkerEnd = Vector3.zero;
        Quaternion stalkerStartRotation = Quaternion.identity;
        Quaternion stalkerEndRotation = Quaternion.identity;

        bool moveStalker = stalker != null && cam != null;

        if (moveStalker)
        {
            stalkerStart = stalker.position;
            stalkerStartRotation = stalker.rotation;

            // Nur waagerecht auf den Spieler zu
            Vector3 toCamera = cam.position - stalkerStart;
            toCamera.y = 0f;

            float distance = toCamera.magnitude;

            Vector3 direction = distance > 0.001f
                ? toCamera / distance
                : stalker.forward;

            stalkerEnd = stalkerStart + direction * Mathf.Max(0f, distance - stopDistance);
            stalkerEndRotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        time = 0f;

        while (time < grabTime)
        {
            time += Time.deltaTime;

            float k = Mathf.Clamp01(time / grabTime);

            if (moveStalker)
            {
                // Beschleunigt auf den Spieler zu
                stalker.position = Vector3.Lerp(stalkerStart, stalkerEnd, k * k);

                stalker.rotation = Quaternion.Slerp(
                    stalkerStartRotation,
                    stalkerEndRotation,
                    Mathf.Clamp01(k * 3f)
                );
            }

            LookAtStalker(cam);

            yield return null;
        }


        // --------------------------------------------------
        // 4) Hände vor der Kamera - danach übernimmt das Ending
        // --------------------------------------------------

        time = 0f;

        while (time < blackoutDelay)
        {
            time += Time.deltaTime;

            LookAtStalker(cam);

            yield return null;
        }
    }


    // ==================================================
    // SPIELER
    // ==================================================

    private void FindMissingReferences()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<CameraController>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }


    private Transform FreezePlayer()
    {
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        if (playerController == null)
            return null;

        playerController.enabled = false;

        // Keine weiteren Interaktionen (E) während der Sequenz
        PickupItem pickupItem = playerController.GetComponentInChildren<PickupItem>();

        if (pickupItem != null)
        {
            pickupItem.enabled = false;
        }

        Rigidbody rb = playerController.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.isKinematic = true;
        }

        return playerController.transform;
    }


    // Position, an die der Spieler vor der Tür geschoben wird
    private Vector3 GetPlayerTarget(Vector3 playerPosition, Transform cam)
    {
        if (victimPoint == null || cam == null)
            return playerPosition;

        Vector3 local = victimPoint.InverseTransformPoint(cam.position);

        local.x = Mathf.Clamp(local.x, -victimSideTolerance, victimSideTolerance);
        local.z = Mathf.Clamp(local.z, -victimFarTolerance, victimNearTolerance);

        Vector3 offset = victimPoint.TransformPoint(local) - cam.position;

        // Höhe des Spielers bleibt gleich
        offset.y = 0f;

        return playerPosition + offset;
    }


    // ==================================================
    // KAMERA
    // ==================================================

    private void LookAtStalker(Transform cam)
    {
        if (cam == null)
            return;

        Vector3 target;

        if (stalkerHead != null)
        {
            target = stalkerHead.TransformPoint(0f, lookHeightOffset, 0f);
        }
        else if (stalker != null)
        {
            target = stalker.position + Vector3.up * 1.9f;
        }
        else
        {
            return;
        }

        Vector3 direction = target - cam.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion wantedRotation = Quaternion.LookRotation(direction, Vector3.up);

        float k = 1f - Mathf.Exp(-cameraTurnSpeed * Time.deltaTime);

        cam.rotation = Quaternion.Slerp(cam.rotation, wantedRotation, k);

        // Taschenlampe folgt dem Blick
        if (cameraController != null && cameraController.hand != null)
        {
            cameraController.hand.rotation = cam.rotation;
        }
    }
}
