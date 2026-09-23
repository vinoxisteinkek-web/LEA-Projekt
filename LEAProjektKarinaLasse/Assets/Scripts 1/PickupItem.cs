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
        // E = Aufheben
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldItem == null)
            {
                TryPickup();
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
    }

    void TryPickup()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
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

        // Position vor dem Spieler berechnen
        Vector3 dropPosition = transform.position + transform.forward * 1.5f;

        // Y-Höhe fest auf 1 setzen
        dropPosition.y = 6;

        // Gegenstand dort ablegen
        heldItem.transform.position = dropPosition;

        heldItem.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

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
}