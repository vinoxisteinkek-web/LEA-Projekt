using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Vector3 dropPoint;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;

    [Header("Sweep Settings")]
    [SerializeField] private float sweepCooldown = 0.5f;

    [Header("Sweep Sounds")]
    [SerializeField] private AudioSource sweepAudioSource;
    [SerializeField] private AudioClip broomSweepSound;
    [SerializeField] private AudioClip mopSweepSound;

    [Header("Door Settings")]
    [SerializeField] private float lockedDoorRange = 1.5f;

    private GameObject heldItem;

    private float nextSweepTime = 0f;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldItem == null)
            {
                TryPickup();
                InteractWithLockedDoor();
                InteractWithLightSwitch();
                InteractWithTaskList();
                InteractWithRadio();
                InteractWithAfterMarketEnding();
                InteractWithDoor();
            }
            else
            {
                ThrowTrashAway();
            }
        }


        // Q = Ablegen
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (heldItem != null)
            {
                DropItem();
            }
        }


        // Wischen
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time < nextSweepTime)
                return;

            bool swept = false;

            if (heldItem != null)
            {
                if (heldItem.name == "brooms")
                {
                    swept = SweepDirt();
                }
                else if (heldItem.name == "Mop")
                {
                    swept = SweepPuddle();
                }
            }

            if (swept)
            {
                nextSweepTime = Time.time + sweepCooldown;
            }
        }
    }


    // ==================================================
    // PICKUP
    // ==================================================

    void TryPickup()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.SphereCast(
            ray,
            0.25f,
            out hit,
            pickupRange,
            ~0,
            QueryTriggerInteraction.Collide))
        {
            if (!hit.collider.CompareTag("Pickup"))
            {
                return;
            }

            heldItem = hit.collider.gameObject;

            heldItem.transform.SetParent(holdPoint);

            heldItem.transform.localPosition = Vector3.zero;
            heldItem.transform.localRotation = Quaternion.identity;

            Rigidbody rb = heldItem.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
            }

            Collider col = heldItem.GetComponent<Collider>();

            if (col != null)
            {
                col.enabled = false;
            }
        }
    }


    // ==================================================
    // DROP
    // ==================================================

    void DropItem()
    {
        heldItem.transform.SetParent(null);

        Vector3 dropPosition =
            transform.position + transform.forward * 1.5f;

        dropPosition.y = 6;

        heldItem.transform.position = dropPosition;

        heldItem.transform.rotation =
            Quaternion.Euler(-90f, 0f, 0f);

        Rigidbody rb = heldItem.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        Collider col = heldItem.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = true;
        }

        heldItem = null;
    }


    // ==================================================
    // MÜLL
    // ==================================================

    void ThrowTrashAway()
    {
        if (heldItem == null || heldItem.name != "trash")
        {
            return;
        }

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.Raycast(
            ray,
            out hit,
            pickupRange,
            ~0,
            QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("dumpster"))
            {
                heldItem.SetActive(false);

                PlayerController player =
                    GetComponentInParent<PlayerController>();

                if (player != null)
                {
                    player.trashCount++;

                    Debug.Log(
                        "Müll entsorgt! Count: " +
                        player.trashCount
                    );

                    if (player.trashCount == 2)
                    {
                        if (StoryManagerStore.Instance != null)
                        {
                            StoryManagerStore.Instance
                                .SecondTrashBagThrownAway();
                        }
                    }
                }
                else
                {
                    Debug.LogError(
                        "PickupItem: PlayerController konnte nicht gefunden werden!"
                    );
                }

                TaskUI taskUI =
                    FindFirstObjectByType<TaskUI>();

                if (taskUI != null)
                {
                    taskUI.UpdateTasks();
                }

                heldItem = null;
            }
        }
    }


    // ==================================================
    // BESEN
    // ==================================================

    bool SweepDirt()
    {
        if (heldItem == null)
            return false;

        if (heldItem.name != "brooms")
            return false;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.SphereCast(
            ray,
            0.25f,
            out hit,
            pickupRange,
            ~0,
            QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("Dirt"))
            {
                DirtPile dirtPile =
                    hit.collider.GetComponentInParent<DirtPile>();

                if (dirtPile != null)
                {
                    dirtPile.Sweep();

                    // Besen-Sound
                    if (sweepAudioSource != null &&
                        broomSweepSound != null)
                    {
                        sweepAudioSource.PlayOneShot(
                            broomSweepSound
                        );
                    }

                    return true;
                }
            }
        }

        return false;
    }


    // ==================================================
    // MOP
    // ==================================================

    bool SweepPuddle()
    {
        if (heldItem == null)
            return false;

        if (heldItem.name != "Mop")
            return false;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.SphereCast(
            ray,
            0.25f,
            out hit,
            pickupRange,
            ~0,
            QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("puddle"))
            {
                Puddles puddle =
                    hit.collider.GetComponentInParent<Puddles>();

                if (puddle != null)
                {
                    puddle.Sweep();

                    // Mop-Sound
                    if (sweepAudioSource != null &&
                        mopSweepSound != null)
                    {
                        sweepAudioSource.PlayOneShot(
                            mopSweepSound
                        );
                    }

                    return true;
                }
            }
        }

        return false;
    }


    // ==================================================
    // LOCKED DOOR
    // ==================================================

    void InteractWithLockedDoor()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.SphereCast(
            ray,
            0.25f,
            out hit,
            lockedDoorRange,
            ~0,
            QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("LockedDoor"))
            {
                LockedDoor door =
                    hit.collider.GetComponentInParent<LockedDoor>();

                if (door != null)
                {
                    door.Interact();
                }
            }
        }
    }


    // ==================================================
    // LICHTSCHALTER
    // ==================================================

    void InteractWithLightSwitch()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.SphereCast(
            ray,
            0.25f,
            out hit,
            pickupRange,
            ~0,
            QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("LightSwitch"))
            {
                LightSwitch lightSwitch =
                    hit.collider.GetComponentInParent<LightSwitch>();

                if (lightSwitch != null)
                {
                    lightSwitch.Interact();
                }
            }
        }
    }


    // ==================================================
    // TASKLISTE
    // ==================================================

    void InteractWithTaskList()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.SphereCast(
            ray,
            0.25f,
            out hit,
            pickupRange,
            ~0,
            QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("TaskList"))
            {
                TaskList taskList =
                    hit.collider.GetComponentInParent<TaskList>();

                if (taskList != null)
                {
                    taskList.Interact();
                }
            }
        }
    }


    // ==================================================
    // RADIO
    // ==================================================

    void InteractWithRadio()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.SphereCast(
            ray,
            0.25f,
            out hit,
            pickupRange,
            ~0,
            QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("Radio"))
            {
                Radio radio =
                    hit.collider.GetComponentInParent<Radio>();

                if (radio != null)
                {
                    radio.Interact();
                }
            }
        }
    }
    void InteractWithAfterMarketEnding()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.SphereCast(
            ray,
            0.25f,
            out hit,
            pickupRange,
            ~0,
            QueryTriggerInteraction.Collide))
        {
            AfterMarketEndingInteraction ending =
                hit.collider.GetComponentInParent<AfterMarketEndingInteraction>();

            if (ending != null)
            {
                ending.Interact();
            }
        }
    }

    void InteractWithDoor()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.SphereCast(
            ray,
            0.25f,
            out hit,
            pickupRange,
            ~0,
            QueryTriggerInteraction.Collide))
        {
            DoorInteraction door =
                hit.collider.GetComponentInParent<DoorInteraction>();

            if (door == null)
                return;

            if (!hit.collider.CompareTag("DoorTrigger") &&
                !hit.collider.transform.root.CompareTag("DoorTrigger"))
            {
                return;
            }

            door.Interact();
        }
    }
}