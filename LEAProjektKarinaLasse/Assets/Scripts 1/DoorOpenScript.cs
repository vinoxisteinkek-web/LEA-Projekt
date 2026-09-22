using UnityEngine;
using TMPro;

public class DoorOpenScript : MonoBehaviour
{
    public Transform door;
    public TextMeshProUGUI interactText;

    public float openAngle = 92f;
    public float openSpeed = 3f;

    private bool playerInside = false;
    private bool doorOpen = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        // Geschlossene Rotation speichern
        closedRotation = door.localRotation;

        // Öffnungsrotation
        openRotation = Quaternion.Euler(
            -90f,
            openAngle,
            0f
        );

        // E am Anfang verstecken
        interactText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Spieler ist im Bereich
        if (playerInside)
        {
            // Tür öffnen/schließen
            if (Input.GetKeyDown(KeyCode.E))
            {
                doorOpen = !doorOpen;
            }
        }

        // Tür bewegen
        if (doorOpen)
        {
            door.localRotation = Quaternion.Slerp(
                door.localRotation,
                openRotation,
                openSpeed * Time.deltaTime
            );
        }
        else
        {
            door.localRotation = Quaternion.Slerp(
                door.localRotation,
                closedRotation,
                openSpeed * Time.deltaTime
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;

            // E anzeigen
            interactText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;

            // E verstecken
            interactText.gameObject.SetActive(false);
        }
    }
}