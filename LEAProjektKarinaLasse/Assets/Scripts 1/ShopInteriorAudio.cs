using UnityEngine;

public class ShopInteriorAudio : MonoBehaviour
{
    [SerializeField] private StoryManagerStore storyManager;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (storyManager != null)
        {
            storyManager.EnterShop();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (storyManager != null)
        {
            storyManager.ExitShop();
        }
    }
}