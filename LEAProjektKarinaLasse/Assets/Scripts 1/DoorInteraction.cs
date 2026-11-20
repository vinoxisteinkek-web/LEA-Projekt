using System;
using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 3f;

    [Header("Door Sounds")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorSound;

    // Optional: eigener Sound beim Schließen. Leer = doorSound wird benutzt.
    [SerializeField] private AudioClip closeSound;

    [Header("Abgeschlossen")]
    [Tooltip("Tür lässt sich nicht öffnen")]
    [SerializeField] private bool isLocked = false;
    [Tooltip("Optional: Rüttel-Sound, wenn man an der abgeschlossenen Tür zieht")]
    [SerializeField] private AudioClip lockedSound;

    [Header("House Door")]
    [SerializeField] private bool isHouseDoor = false;
    [SerializeField] private AudioClip houseDoorSound;

    private bool isOpen = false;
    private bool isMoving = false;

    // Die Story an der Haustür soll nur beim ersten Öffnen starten
    private bool houseDoorTriggered = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Quaternion targetRotation;

    // Kollision Tür <-> Spieler ist beim Schließen aus,
    // damit die Tür den Spieler nicht wegschiebt
    private Collider[] doorColliders;
    private Collider[] playerColliders;
    private bool playerCollisionIgnored = false;

    public bool IsOpen => isOpen;

    // Andere Scripts können die Tür ab-/aufschließen
    public bool IsLocked
    {
        get => isLocked;
        set => isLocked = value;
    }

    // Wird aufgerufen, wenn jemand die abgeschlossene Tür öffnen will
    public event Action OnLockedAttempt;

    // Wird aufgerufen, sobald die Tür aufgeht
    public event Action OnOpened;

    private void Start()
    {
        closedRotation = transform.localRotation;

        openRotation = closedRotation * Quaternion.Euler(
            0f,
            0f,
            openAngle
        );

        targetRotation = closedRotation;

        doorColliders = GetSolidColliders(gameObject);
    }

    // Öffnet die Tür, wenn sie zu ist, und schließt sie, wenn sie offen ist.
    // Drückt man während der Bewegung, dreht die Tür einfach um.
    public void Interact()
    {
        // Abgeschlossen: nur Rütteln, Tür bleibt zu
        if (!isOpen && isLocked)
        {
            if (audioSource != null && lockedSound != null)
            {
                audioSource.PlayOneShot(lockedSound);
            }

            OnLockedAttempt?.Invoke();
            return;
        }

        isOpen = !isOpen;
        targetRotation = isOpen ? openRotation : closedRotation;
        isMoving = true;

        if (!isOpen)
        {
            SetPlayerCollision(false);
        }

        Debug.Log(isOpen ? "Tür wurde geöffnet." : "Tür wurde geschlossen.");

        // Tür-Sound (beim Schließen closeSound, falls gesetzt)
        AudioClip clip = (!isOpen && closeSound != null) ? closeSound : doorSound;

        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }

        if (isOpen)
        {
            OnOpened?.Invoke();
        }

        // Haustür: Zusatz-Sound und Story nur beim ersten Öffnen
        if (isOpen && isHouseDoor && !houseDoorTriggered)
        {
            houseDoorTriggered = true;

            if (audioSource != null && houseDoorSound != null)
            {
                audioSource.PlayOneShot(houseDoorSound);
            }

            if (StoryManagerAfterMarket.Instance != null)
            {
                Debug.Log("AfterMarket Story: EnterHouse() wird aufgerufen.");

                StoryManagerAfterMarket.Instance.EnterHouse();
            }
        }
    }

    private void Update()
    {
        if (!isMoving)
        {
            if (playerCollisionIgnored && !IsOverlappingPlayer())
            {
                SetPlayerCollision(true);
            }

            return;
        }

        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            targetRotation,
            openSpeed * 90f * Time.deltaTime
        );

        if (Quaternion.Angle(
            transform.localRotation,
            targetRotation
        ) < 0.1f)
        {
            transform.localRotation = targetRotation;
            isMoving = false;
        }
    }

    // =========================
    // KOLLISION MIT DEM SPIELER
    // =========================

    private void SetPlayerCollision(bool enabled)
    {
        if (playerColliders == null || playerColliders.Length == 0)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();

            if (player == null)
                return;

            playerColliders = GetSolidColliders(player.gameObject);
        }

        foreach (Collider door in doorColliders)
        {
            foreach (Collider player in playerColliders)
            {
                if (door != null && player != null)
                    Physics.IgnoreCollision(door, player, !enabled);
            }
        }

        playerCollisionIgnored = !enabled;
    }

    private bool IsOverlappingPlayer()
    {
        if (playerColliders == null)
            return false;

        foreach (Collider door in doorColliders)
        {
            foreach (Collider player in playerColliders)
            {
                if (door == null || player == null)
                    continue;

                if (Physics.ComputePenetration(
                    door, door.transform.position, door.transform.rotation,
                    player, player.transform.position, player.transform.rotation,
                    out _, out _))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Collider[] GetSolidColliders(GameObject root)
    {
        return System.Array.FindAll(
            root.GetComponentsInChildren<Collider>(true),
            c => !c.isTrigger
        );
    }
}
