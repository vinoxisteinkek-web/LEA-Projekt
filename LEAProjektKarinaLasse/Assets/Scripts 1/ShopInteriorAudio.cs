using UnityEngine;

public class ShopInteriorAudio : MonoBehaviour
{
    [SerializeField] private StoryManagerStore storyManager;
    [SerializeField] private bool enterShop = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (storyManager == null)
            return;

        if (enterShop)
        {
            storyManager.EnterShop();
        }
        else
        {
            storyManager.ExitShop();
        }
    }
}