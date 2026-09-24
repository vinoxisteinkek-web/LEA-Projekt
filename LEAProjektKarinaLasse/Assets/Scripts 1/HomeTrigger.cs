using UnityEngine;

public class HomeTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (StoryManagerStore.Instance != null)
        {
            StoryManagerStore.Instance.ReachedHomeTrigger();
        }
    }
}