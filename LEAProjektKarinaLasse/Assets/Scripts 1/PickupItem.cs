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
        //Wischen
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

    void TryPickup()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        // SphereCast statt normalem Raycast
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

            // Gegenstand an die Hand hängen
            heldItem.transform.SetParent(holdPoint);

            // Position an HoldPoint setzen
            heldItem.transform.localPosition = Vector3.zero;
            heldItem.transform.localRotation = Quaternion.identity;

            // Physik ausschalten
            Rigidbody rb = heldItem.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // Collider ausschalten
            Collider col = heldItem.GetComponent<Collider>();

            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    void DropItem()
    {
        // Gegenstand vom Spieler lösen
        heldItem.transform.SetParent(null);

        // Position vor dem Spieler
        Vector3 dropPosition =
            transform.position + transform.forward * 1.5f;

        dropPosition.y = 6;

        heldItem.transform.position = dropPosition;

        heldItem.transform.rotation =
            Quaternion.Euler(-90f, 0f, 0f);

        // Physik wieder einschalten
        Rigidbody rb = heldItem.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Collider wieder einschalten
        Collider col = heldItem.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = true;
        }

        heldItem = null;
    }

    void ThrowTrashAway()
    {
        // Nur Müllbeutel dürfen in den Dumpster
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
                // Müll verschwinden lassen
                heldItem.SetActive(false);

                // PlayerController suchen
                PlayerController player =
                    GetComponentInParent<PlayerController>();

                if (player != null)
                {
                    // Müll zählen
                    player.trashCount++;

                    Debug.Log("Müll entsorgt! Count: " + player.trashCount);

                    // Beim zweiten Müllsack Story-Event starten
                    if (player.trashCount == 2)
                    {
                        if (StoryManagerStore.Instance != null)
                        {
                            StoryManagerStore.Instance.SecondTrashBagThrownAway();
                        }
                    }
                }
                else
                {
                    Debug.LogError(
                        "PickupItem: PlayerController konnte nicht gefunden werden!"
                    );
                }

                // Task UI aktualisieren
                TaskUI taskUI =
                    FindFirstObjectByType<TaskUI>();

                if (taskUI != null)
                {
                    taskUI.UpdateTasks();
                }

                // Kein Gegenstand mehr in der Hand
                heldItem = null;
            }
        }
    }

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
                    return true;
                }
            }
        }

        return false;
    }
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
                    return true;
                }
            }
        }

        return false;
    }

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
}