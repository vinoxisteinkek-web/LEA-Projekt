using UnityEngine;

public class Radio : MonoBehaviour
{
    private bool gotTurnedOff = false;

    public void Interact()
    {
        // Ohne StoryManager (z. B. Radio in einer anderen Szene) nichts tun
        if (StoryManagerStore.Instance == null)
            return;

        if (!StoryManagerStore.Instance.RadioIsOn())
        {
            if (!gotTurnedOff)
                StoryManagerStore.Instance.TurnOnRadio();

            return;
        }


        if (StoryManagerStore.Instance.CanTurnRadioOff())
        {
            gotTurnedOff = true;
            StoryManagerStore.Instance.StopRadio();
        }
    }
}
