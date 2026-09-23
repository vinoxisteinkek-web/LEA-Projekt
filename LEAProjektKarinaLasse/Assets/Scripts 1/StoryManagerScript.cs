using UnityEngine;

public class StoryManagerScript : MonoBehaviour
{
    public static StoryManagerScript Instance;

    public int storyStep = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
