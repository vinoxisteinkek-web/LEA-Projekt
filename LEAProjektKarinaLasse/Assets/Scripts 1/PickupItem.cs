using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Vector3 dropPoint;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;

    private GameObject heldItem;

    void Update()
    {
        // E = Aufheben / Müll wegwerfen
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldItem == null)
            {
                TryPickup();
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

        // Linksklick = Wischen
        if (Input.GetMouseButtonDown(0))
        {
            SweepDirt();
            SweepPuddle();
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
        if (heldItem.name != "trash")
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
                // Müllbeutel deaktivieren
                heldItem.SetActive(false);

                // Hand wieder frei
                heldItem = null;
            }
        }
    }

    void SweepDirt()
    {
        if (heldItem == null)
            return;

        // Nur Besen
        if (heldItem.name != "brooms")
            return;

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
                }
            }
        }
    }

    void SweepPuddle()
    {
        if (heldItem == null)
            return;

        // Nur Mop
        if (heldItem.name != "Mop")
            return;

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
                }
            }
        }
    }
}