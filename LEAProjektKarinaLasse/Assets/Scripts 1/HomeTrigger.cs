using UnityEngine;

public class HomeTrigger : MonoBehaviour
{
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (StoryManagerStore.Instance == null)
            return;

        triggered = true;

        StoryManagerStore.Instance.ReachedHomeTrigger();
    }
}