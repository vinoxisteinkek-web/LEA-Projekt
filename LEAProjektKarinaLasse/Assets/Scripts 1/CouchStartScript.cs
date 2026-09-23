using UnityEngine;
using System.Collections;

public class CouchStartScript : MonoBehaviour
{
    public PlayerController playerController;
    public CameraController cameraController;

    public Transform standingPosition;

    public GameObject interactText;

    public StoryMessageScript storyMessage;

    // Die beiden Essen
    public GameObject pizza;
    public GameObject cereal;

    private Rigidbody rb;
    private bool canStand = true;

    private void Start()
    {
        rb = playerController.GetComponent<Rigidbody>();

        playerController.enabled = false;
        cameraController.enabled = false;

        rb.isKinematic = true;
        rb.useGravity = false;

        interactText.SetActive(true);
    }

    public void StandUp()
    {
        if (!canStand)
            return;

        canStand = false;

        playerController.transform.position = standingPosition.position;

        playerController.transform.rotation = Quaternion.Euler(
            0f,
            standingPosition.eulerAngles.y,
            0f
        );

        rb.isKinematic = false;
        rb.useGravity = true;

        playerController.enabled = true;
        cameraController.enabled = true;

        interactText.SetActive(false);

        // Erster Text
        storyMessage.ShowMessage("Oh no... i have to be at work soon!");

        StartCoroutine(FoodMessage());
    }

    private IEnumerator FoodMessage()
    {
        yield return new WaitForSeconds(5f);

        storyMessage.ShowMessage("But first i gotta eat something.");

        yield return new WaitForSeconds(4f);

        storyMessage.ShowMessage("Hmmm... Pizza or cereal?");
    }

    public void EatPizza()
    {
        pizza.SetActive(false);

        storyMessage.ShowMessage("Pizza... yummy.");

        // Später können wir hier den nächsten Story-Schritt starten
    }

    public void EatCereal()
    {
        cereal.SetActive(false);

        storyMessage.ShowMessage("Cereal... yummyS.");

        // Später können wir hier den nächsten Story-Schritt starten
    }
}