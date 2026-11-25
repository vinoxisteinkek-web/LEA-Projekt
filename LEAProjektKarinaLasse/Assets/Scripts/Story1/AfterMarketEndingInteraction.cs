using UnityEngine;

public class AfterMarketEndingInteraction : MonoBehaviour
{
    private enum EndingType
    {
        Phone,
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

                StoryManagerAfterMarket.Instance
                    .InteractWithPhone();

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