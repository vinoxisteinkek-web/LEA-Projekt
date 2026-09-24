using UnityEngine;

public class Radio : MonoBehaviour
{
    public void Interact()
    {
        if (!StoryManagerStore.Instance.RadioIsOn())
        {
            StoryManagerStore.Instance.TurnOnRadio();
            return;
        }

        if (StoryManagerStore.Instance.CanTurnRadioOff())
        {
            StoryManagerStore.Instance.StopRadio();
        }
    }
}