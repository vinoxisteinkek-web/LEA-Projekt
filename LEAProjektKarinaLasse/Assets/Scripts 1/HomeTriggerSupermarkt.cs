using UnityEngine;

public class HomeTriggerSupermarkt : MonoBehaviour
{
   
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            if (StoryManagerStore.Instance != null)
            {
                StoryManagerStore.Instance.ReachedHomeTrigger();
            }
            else
            {
                Debug.LogError("StoryManagerStore wurde nicht gefunden!");
            }
        }
    }
}