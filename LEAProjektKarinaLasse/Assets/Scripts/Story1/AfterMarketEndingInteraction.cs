using UnityEngine;

public class AfterMarketEndingInteraction : MonoBehaviour
{
    private enum EndingType
    {
        Phone,      // Police-Ending entfernt - Wert bleibt, damit die Szene ihre Einstellungen behaelt
        Bed,
        KillerDoor
    }

    [SerializeField] private EndingType endingType;


    public void Interact()
    {
        if (StoryManagerAfterMarket.Instance == null)
            return;


        switch (endingType)
        {
            case EndingType.Phone:

                // Das Telefon ist nur noch Deko (kein Ending mehr).

                break;


            case EndingType.Bed:

                StoryManagerAfterMarket.Instance
                    .InteractWithBed();

                break;


            case EndingType.KillerDoor:

                StoryManagerAfterMarket.Instance
                    .InteractWithKillerDoor();

                break;
        }
    }
}
