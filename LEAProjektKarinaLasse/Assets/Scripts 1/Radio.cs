using UnityEngine;

public class Radio : MonoBehaviour
{
    private bool gotTurnedOff = false;
    public void Interact()
    {
        if (!StoryManagerStore.Instance.RadioIsOn())
        {
            if(!gotTurnedOff)
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