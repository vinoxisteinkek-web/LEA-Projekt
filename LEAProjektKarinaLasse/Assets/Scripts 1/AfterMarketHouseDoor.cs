using UnityEngine;

public class AfterMarketHouseDoor : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private bool doorOpened = false;

    public void Interact()
    {
        if (doorOpened)
            return;

        doorOpened = true;

        Debug.Log("Eingangstür wurde geöffnet.");

        if (StoryManagerAfterMarket.Instance != null)
        {
            StoryManagerAfterMarket.Instance.EnterHouse();
        }
    }
}