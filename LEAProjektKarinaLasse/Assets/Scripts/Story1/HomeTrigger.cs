using UnityEngine;

public class HomeTrigger : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
            
            if (other.CompareTag("Player"))
                StoryManagerStore.Instance.ReachedHomeTrigger();
            return;

      
    }
}