using UnityEngine;
using System.Collections;
using TMPro;

public class CouchStartScript : MonoBehaviour
{
    public PlayerController playerController;
    public CameraController cameraController;
    public PlayerInteractionScript playerInteraction;

    public Transform standingPosition;

    // E-Text zum Aufstehen
    public GameObject interactText;
    public TextMeshProUGUI interactTextUI;

    public StoryMessageScript storyMessage;

    // Essen
    public GameObject pizza;
    public GameObject cereal;

    // Trigger
    public GameObject wardrobeTrigger;
    public GameObject dryerTrigger;

    // Schwarzer Bildschirm
    public GameObject blackScreen;
    public CanvasGroup blackScreenCanvas;

    private Rigidbody rb;

    private bool canStand = false;
    private bool canEat = false;
    private bool foodEaten = false;


    // =====================================
    // SOUNDS
    // =====================================

    // Sound beim Essen
    public AudioSource eatSound;

    // Sound für klemmende Tür
    public AudioSource doorJammedSound;

    // Sound beim Umziehen
    public AudioSource changeClothesSound;


    private void Start()
    {
        rb = playerController.GetComponent<Rigidbody>();

        // =====================================
        // SPIELER SITZT AM ANFANG
        // =====================================

        playerController.enabled = false;
        cameraController.enabled = false;

        rb.isKinematic = true;
        rb.useGravity = false;

        // E-Text am Anfang verstecken
        interactText.SetActive(false);

        // Text vorbereiten
        interactTextUI.text = "E - To stand up";

        // Kleiderschrank und Trockner deaktivieren
        wardrobeTrigger.SetActive(false);
        dryerTrigger.SetActive(false);

        // =====================================
        // SCHWARZER BILDSCHIRM
        // =====================================

        blackScreen.SetActive(true);
        blackScreenCanvas.alpha = 1f;

        // =====================================
        // AUFWACHEN
        // =====================================

        StartCoroutine(WakeUp());
    }


    // =====================================
    // AUFWACHEN
    // =====================================

    private IEnumerator WakeUp()
    {
        blackScreen.SetActive(true);

        // Komplett schwarz starten
        blackScreenCanvas.alpha = 1f;

        float fadeDuration = 3f;
        float time = 0f;

        // Langsam von schwarz zu sichtbar
        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float alpha = 1f - (time / fadeDuration);

            blackScreenCanvas.alpha = alpha;

            yield return null;
        }

        // Am Ende komplett durchsichtig
        blackScreenCanvas.alpha = 0f;
        blackScreen.SetActive(false);


        // Erster Text
        storyMessage.ShowMessage(
            "Oh nein... ich muss doch zur Arbeit!"
        );


        // Text 4 Sekunden anzeigen
        yield return new WaitForSeconds(4f);

        storyMessage.HideMessage();


        // E zum Aufstehen anzeigen
        canStand = true;

        interactTextUI.text = "E - To stand up";
        interactTextUI.gameObject.SetActive(true);
    }


    // =====================================
    // AUFSTEHEN
    // =====================================

    public void StandUp()
    {
        if (!canStand)
            return;

        canStand = false;

        // E-Text ausblenden
        interactText.SetActive(false);


        // Spieler auf stehende Position setzen
        playerController.transform.position = standingPosition.position;


        // Nur Y-Rotation übernehmen
        playerController.transform.rotation = Quaternion.Euler(
            0f,
            standingPosition.eulerAngles.y,
            0f
        );


        // Physik wieder aktivieren
        rb.isKinematic = false;
        rb.useGravity = true;


        // Spielersteuerung aktivieren
        playerController.enabled = true;
        cameraController.enabled = true;


        // Essen-Text starten
        StartCoroutine(FoodMessage());
    }


    // =====================================
    // ESSEN
    // =====================================

    private IEnumerator FoodMessage()
    {
        yield return new WaitForSeconds(4f);

        storyMessage.ShowMessage(
            "Aber zuerst muss ich noch schnell etwas essen."
        );

        yield return new WaitForSeconds(4f);

        storyMessage.ShowMessage(
            "Hmmm... Pizza or cereal?"
        );

        canEat = true;
    }


    // =====================================
    // PIZZA ESSEN
    // =====================================

    public void EatPizza()
    {
        if (!canEat || foodEaten)
            return;

        foodEaten = true;
        canEat = false;


        // E-Text entfernen
        playerInteraction.ClearInteraction();


        // Pizza verschwinden lassen
        pizza.SetActive(false);


        // Essen Sound
        if (eatSound != null)
        {
            eatSound.Play();
        }


        // Pizza Collider deaktivieren
        Collider pizzaCollider = pizza.GetComponent<Collider>();

        if (pizzaCollider != null)
        {
            pizzaCollider.enabled = false;
        }


        // Cereal ebenfalls deaktivieren
        Collider cerealCollider = cereal.GetComponent<Collider>();

        if (cerealCollider != null)
        {
            cerealCollider.enabled = false;
        }


        storyMessage.ShowMessage(
            "Pizza... lecker."
        );


        StartCoroutine(AfterEating());
    }


    // =====================================
    // CEREAL ESSEN
    // =====================================

    public void EatCereal()
    {
        if (!canEat || foodEaten)
            return;

        foodEaten = true;
        canEat = false;


        // E-Text entfernen
        playerInteraction.ClearInteraction();


        // Cereal verschwinden lassen
        cereal.SetActive(false);


        // Essen Sound
        if (eatSound != null)
        {
            eatSound.Play();
        }


        // Cereal Collider deaktivieren
        Collider cerealCollider = cereal.GetComponent<Collider>();

        if (cerealCollider != null)
        {
            cerealCollider.enabled = false;
        }


        // Pizza ebenfalls deaktivieren
        Collider pizzaCollider = pizza.GetComponent<Collider>();

        if (pizzaCollider != null)
        {
            pizzaCollider.enabled = false;
        }


        storyMessage.ShowMessage(
            "Cereal... lecker."
        );


        StartCoroutine(AfterEating());
    }


    // =====================================
    // NACH DEM ESSEN
    // =====================================

    private IEnumerator AfterEating()
    {
        yield return new WaitForSeconds(3f);


        storyMessage.ShowMessage(
            "Okay... jetzt muss ich mich noch schnell umziehen."
        );


        // Kleiderschrank aktivieren
        wardrobeTrigger.SetActive(true);
    }


    // =====================================
    // KLEIDERSCHRANK
    // =====================================

    public void OpenWardrobe()
    {
        // E-Text entfernen
        playerInteraction.ClearInteraction();


        // Kleiderschrank deaktivieren
        wardrobeTrigger.SetActive(false);


        // =====================================
        // SOUND: TÜR KLEMMT
        // =====================================

        if (doorJammedSound != null)
        {
            doorJammedSound.Play();
        }


        storyMessage.ShowMessage(
            "Oh nein... die Tür klemmt."
        );


        StartCoroutine(WardrobeMessage());
    }


    private IEnumerator WardrobeMessage()
    {
        yield return new WaitForSeconds(3f);


        storyMessage.ShowMessage(
            "Ach stimmt... ich habe noch eine Uniform im Trockner."
        );


        yield return new WaitForSeconds(2f);


        // Trockner aktivieren
        dryerTrigger.SetActive(true);
    }


    // =====================================
    // TROCKNER
    // =====================================

    public void UseDryer()
    {
        // E-Text entfernen
        playerInteraction.ClearInteraction();


        // Trockner deaktivieren
        dryerTrigger.SetActive(false);


        // =====================================
        // SOUND: UMZIEHEN
        // =====================================

        if (changeClothesSound != null)
        {
            changeClothesSound.Play();
        }


        StartCoroutine(ChangeClothes());
    }


    // =====================================
    // UMZIEHEN
    // =====================================

    private IEnumerator ChangeClothes()
    {
        // Bildschirm schwarz
        blackScreen.SetActive(true);
        blackScreenCanvas.alpha = 1f;


        // 2 Sekunden schwarz
        yield return new WaitForSeconds(2f);


        // Bildschirm wieder sichtbar
        blackScreenCanvas.alpha = 0f;
        blackScreen.SetActive(false);


        storyMessage.ShowMessage(
            "Okay... jetzt kann ich zur Arbeit."
        );


        yield return new WaitForSeconds(5f);


        // Text verschwinden lassen
        storyMessage.HideMessage();
    }
}