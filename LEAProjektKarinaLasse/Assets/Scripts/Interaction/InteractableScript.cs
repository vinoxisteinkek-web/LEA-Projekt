using UnityEngine;
using UnityEngine.Events;

public class InteractableScript : MonoBehaviour
{
    public string interactionText = "E - Interagieren";

    public int requiredStoryStep = 0;
    public int nextStoryStep = -1;

    public UnityEvent onInteract;

    public bool CanInteract()
    {
        return StoryManagerScript.Instance.storyStep == requiredStoryStep;
    }

    public void Interact()
    {
        onInteract.Invoke();

        if (nextStoryStep >= 0)
        {
            StoryManagerScript.Instance.storyStep = nextStoryStep;
        }
    }
}