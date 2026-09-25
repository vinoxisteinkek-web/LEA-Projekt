using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 3f;

    [Header("Door Sounds")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorSound;

    [Header("House Door")]
    [SerializeField] private bool isHouseDoor = false;
    [SerializeField] private AudioClip houseDoorSound;

    private bool isOpen = false;
    private bool isMoving = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        closedRotation = transform.localRotation;

        openRotation = closedRotation * Quaternion.Euler(
            0f,
            0f,
            openAngle
        );
    }

    public void Interact()
    {
        if (isOpen || isMoving)
            return;

        isOpen = true;
        isMoving = true;

        Debug.Log("Tür wurde geöffnet.");

        // Normaler Tür-Sound
        if (audioSource != null && doorSound != null)
        {
            audioSource.PlayOneShot(doorSound);
        }

        // Zusätzlicher Sound bei der Haustür
        if (isHouseDoor &&
            audioSource != null &&
            houseDoorSound != null)
        {
            audioSource.PlayOneShot(houseDoorSound);
        }

        // Story starten
        if (isHouseDoor &&
            StoryManagerAfterMarket.Instance != null)
        {
            Debug.Log("AfterMarket Story: EnterHouse() wird aufgerufen.");

            StoryManagerAfterMarket.Instance.EnterHouse();
        }
    }

    private void Update()
    {
        if (!isMoving)
            return;

        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            openRotation,
            openSpeed * 90f * Time.deltaTime
        );

        if (Quaternion.Angle(
            transform.localRotation,
            openRotation
        ) < 0.1f)
        {
            transform.localRotation = openRotation;
            isMoving = false;
        }
    }
}