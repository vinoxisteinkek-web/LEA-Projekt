using TMPro;
using UnityEngine;

public class PlayerInteractionScript : MonoBehaviour
{
    public TextMeshProUGUI interactText;

    private InteractableScript currentInteractable;

    private void Start()
    {
        interactText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (currentInteractable != null)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                currentInteractable.Interact();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        InteractableScript interactable = other.GetComponent<InteractableScript>();

        if (interactable != null)
        {
            if (interactable.CanInteract())
            {
                currentInteractable = interactable;

                interactText.text = interactable.interactionText;
                interactText.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        InteractableScript interactable = other.GetComponent<InteractableScript>();

        if (interactable != null && currentInteractable == interactable)
        {
            currentInteractable = null;
            interactText.gameObject.SetActive(false);
        }
    }
}
