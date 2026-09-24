using UnityEngine;

public class FlashlightHintTrigger : MonoBehaviour
{
    public PlayerController playerController;

    private bool alreadyTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (alreadyTriggered)
            return;

        if (other.CompareTag("Player"))
        {
            alreadyTriggered = true;

            playerController.ShowFlashlightHint();
        }
    }
}