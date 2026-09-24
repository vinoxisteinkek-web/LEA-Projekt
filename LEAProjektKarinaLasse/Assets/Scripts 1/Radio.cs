using UnityEngine;

public class Radio : MonoBehaviour
{
    public void Interact()
    {
        if (StoryManagerStore.Instance != null)
        {
            StoryManagerStore.Instance.TurnOnRadio();
        }
    }
}