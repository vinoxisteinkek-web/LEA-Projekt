using UnityEngine;

public class TaskList : MonoBehaviour
{
    public void Interact()
    {
        if (StoryManagerStore.Instance == null)
            return;

        StoryManagerStore.Instance.ReadTaskList();
    }
}