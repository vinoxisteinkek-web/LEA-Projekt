using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;
using System.Collections;

public class StoryManagerAfterMarket : MonoBehaviour
{
    public static StoryManagerAfterMarket Instance;

    // ==================================================
    // SOUNDS
    // ==================================================

    [Header("Sounds")]
    [SerializeField] private AudioSource storyAudioSource;
    [SerializeField] private AudioClip someoneIsInHouseSound;


    // ==================================================
    // ENDING 1 - PHONE
    // ==================================================

    [Header("Ending 1 - Phone")]
    [SerializeField] private AudioSource phoneEndingAudioSource;
    [SerializeField] private AudioClip phoneEndingSound;


    // ==================================================
    // ENDING 2 - BED
    // ==================================================

    [Header("Ending 2 - Bed")]
    [SerializeField] private AudioSource bedEndingAudioSource;
    [SerializeField] private AudioClip bedEndingSound;


    // ==================================================
    // ENDING 3 - KILLER DOOR
    // ==================================================

    [Header("Ending 3 - Killer Door")]
    [SerializeField] private AudioSource killerDoorEndingAudioSource;
    [SerializeField] private AudioClip killerDoorEndingSound;


    // ==================================================
    // DIALOGUE
    // ==================================================

    [Header("Dialogue")]
    [SerializeField] private TextMeshProUGUI dialogueText;

    [SerializeField] private float textSpeed = 0.05f;

    [SerializeField] private float dialogueEndDelay = 1.5f;

    private RectTransform dialogueRect;
    private Vector2 normalDialoguePosition;


    // ==================================================
    // PLAYER VOICE
    // ==================================================

    [Header("Player Voice")]
    [SerializeField] private AudioSource playerVoiceAudioSource;
    [SerializeField] private AudioClip playerTalking;


    // ==================================================
    // POLICE VOICE
    // ==================================================

    [Header("Police Voice")]
    [SerializeField] private AudioSource policeVoiceAudioSource;
    [SerializeField] private AudioClip policeTalking;


    // ==================================================
    // AMBIENT
    // ==================================================

    [Header("Ambient")]
    [SerializeField] private AudioSource ambientHumAudioSource;
    [SerializeField] private AudioSource cicadaAudioSource;

    [SerializeField] private float ambientVolume = 0.03f;


    // ==================================================
    // ENDING SCREEN
    // ==================================================

    [Header("Ending Screen")]
    [SerializeField] private GameObject endingScreen;


    // ==================================================
    // STATUS
    // ==================================================

    private bool enteredHouse = false;
    private bool endingTriggered = false;


    // ==================================================
    // START
    // ==================================================

    private void Awake()
    {
        Instance = this;

        if (storyAudioSource != null)
        {
            storyAudioSource.Stop();
        }

        if (phoneEndingAudioSource != null)
        {
            phoneEndingAudioSource.Stop();
        }

        if (bedEndingAudioSource != null)
        {
            bedEndingAudioSource.Stop();
        }

        if (killerDoorEndingAudioSource != null)
        {
            killerDoorEndingAudioSource.Stop();
        }

        if (playerVoiceAudioSource != null)
        {
            playerVoiceAudioSource.Stop();
        }

        if (dialogueText != null)
        {
            dialogueRect = dialogueText.GetComponent<RectTransform>();
            normalDialoguePosition = dialogueRect.anchoredPosition;

            dialogueText.gameObject.SetActive(false);
        }

        if (ambientHumAudioSource != null)
        {
            ambientHumAudioSource.loop = true;
            ambientHumAudioSource.volume = 0f;
        }

        if (cicadaAudioSource != null)
        {
            cicadaAudioSource.loop = true;
            cicadaAudioSource.volume = 0f;
        }

        if (endingScreen != null)
        {
            endingScreen.SetActive(false);
        }
    }


    private void Start()
    {
        StartAmbientSounds();
    }


    // ==================================================
    // AMBIENT SOUNDS
    // ==================================================

    private void StartAmbientSounds()
    {
        if (ambientHumAudioSource != null)
        {
            ambientHumAudioSource.loop = true;
            ambientHumAudioSource.volume = ambientVolume;

            if (!ambientHumAudioSource.isPlaying)
            {
                ambientHumAudioSource.Play();
            }
        }

        if (cicadaAudioSource != null)
        {
            cicadaAudioSource.loop = true;
            cicadaAudioSource.volume = ambientVolume;

            if (!cicadaAudioSource.isPlaying)
            {
                cicadaAudioSource.Play();
            }
        }
    }


    private void StopAmbientSounds()
    {
        if (ambientHumAudioSource != null)
        {
            ambientHumAudioSource.Stop();
            ambientHumAudioSource.volume = 0f;
        }

        if (cicadaAudioSource != null)
        {
            cicadaAudioSource.Stop();
            cicadaAudioSource.volume = 0f;
        }
    }


    // ==================================================
    // EINGANGSTÜR
    // ==================================================

    public void EnterHouse()
    {
        if (enteredHouse)
            return;

        enteredHouse = true;

        Debug.Log("Spieler ist ins Haus gegangen.");

        StopAmbientSounds();

        PlaySound(
            storyAudioSource,
            someoneIsInHouseSound
        );

        StartCoroutine(EnternedHouse());
    }


    private IEnumerator EnternedHouse()
    {
        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(
            PlayerSay(
                "Finally home, i should get some sleep."
            )
        );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PlayerSay(
                "i need to change my clothes first"
            )
        );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PlayerSay(
                "i can't get rid of the feeling that something is wrong."
            )
        );
    }


    // ==================================================
    // PHONE
    // ==================================================

    public void InteractWithPhone()
    {
        if (endingTriggered)
            return;

        endingTriggered = true;

        StopAmbientSounds();

        Debug.Log("ENDING 1 - PHONE");

        StartCoroutine(PhoneEnding());
    }


    private IEnumerator PhoneEnding()
    {
        ShowEndingScreen();
        SetEndingDialoguePosition();


        yield return new WaitForSeconds(1f);
        

        yield return StartCoroutine(
            PlayerSay(
                "Hello? Is anyone there?"
            )
        );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PoliceSay(
                "Hello, this is the police. How can we help you"
            )
        );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PlayerSay(
                "I feel like theres a intruder in my House"
            )
        );

        yield return StartCoroutine(
            PoliceSay(
                "We will send a unit to your location immediately"
            )
        );

        yield return new WaitForSeconds(1f);

        PlaySound(
            phoneEndingAudioSource,
            phoneEndingSound
        );
        
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PoliceSay(
                    "We found a broken window in your house, it seems like the Intruder managed to escape."
                )
            );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PoliceSay(
                 "but we will continue to search the surrounding area."
                )
            );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PlayerSay(
                "I drove to my mother that night and stayed there for a while"
            )
        );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PlayerSay(
                "Ending 1/3"
            )
        );

        yield return new WaitForSeconds(2f);


        SceneManager.LoadScene("MenueScene");
    }


    // ==================================================
    // BED
    // ==================================================

    public void InteractWithBed()
    {
        if (endingTriggered)
            return;

        endingTriggered = true;

        StopAmbientSounds();

        Debug.Log("ENDING 2 - BED");

        StartCoroutine(BedEnding());
    }


    private IEnumerator BedEnding()
    {
        ShowEndingScreen();
        SetEndingDialoguePosition();

        PlaySound(
            bedEndingAudioSource,
            bedEndingSound
        );

        yield return new WaitForSeconds(
            GetSoundLength(bedEndingSound)
        );

        yield return StartCoroutine(
            PlayerSay(
                "Next morning"
            )
        );

        yield return new WaitForSeconds(1f);

        PlaySound(
            phoneEndingAudioSource,
            phoneEndingSound
        );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PoliceSay(
                "We're sorry, there are no signs of Cassy inside the House"
            )
        );

        yield return StartCoroutine(
            PoliceSay(
                "We will continue the search in the surrounding area Emschurches"
            )
        );

        yield return StartCoroutine(
            PoliceSay(
                "We will keep you updated"
            )
        );

        yield return StartCoroutine(
            PlayerSay(
                "Ending 2/3"
            )
        );

        yield return new WaitForSeconds(2f);

        SceneManager.LoadScene("MenueScene");

    }
    // ==================================================
    // KILLER DOOR
    // ==================================================

    public void InteractWithKillerDoor()
    {
        if (endingTriggered)
            return;

        endingTriggered = true;

        StopAmbientSounds();

        Debug.Log("ENDING 3 - KILLER DOOR");

        StartCoroutine(KillerDoorEnding());
    }


    private IEnumerator KillerDoorEnding()
    {
        ShowEndingScreen();
        SetEndingDialoguePosition();

        PlaySound(
            killerDoorEndingAudioSource,
            killerDoorEndingSound
        );

        yield return new WaitForSeconds(
            GetSoundLength(killerDoorEndingSound)
        );

        yield return StartCoroutine(
            PlayerSay(
                "Next Morning"
            )
        );
         
        yield return new WaitForSeconds(1f);

        PlaySound(
            phoneEndingAudioSource,
            phoneEndingSound
        );
         yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PoliceSay(
                "We found the remains of Cassy inside the House"
            )
        );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PoliceSay(
                "It doesn't look like an accident"
            )
        );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PoliceSay(
                "We're sorry for your Loss"
            )
        );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PoliceSay(
                "We will continue the search for the Killer in the surrounding area Emschurches"
            )
        );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            PlayerSay(
                "Ending 3/3"
            )
        );

        yield return new WaitForSeconds(2f);


        SceneManager.LoadScene("MenueScene");
    }


    // ==================================================
    // PLAYER SAY
    // ==================================================

    private IEnumerator PlayerSay(string message)
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.color = new Color32(255, 176, 0, 255);
            dialogueText.gameObject.SetActive(true);
        }

        StartPlayerVoice();

        for (int i = 0; i < message.Length; i++)
        {
            if (dialogueText != null)
            {
                dialogueText.text += message[i];
            }

            yield return new WaitForSeconds(textSpeed);
        }

        StopPlayerVoice();

        yield return new WaitForSeconds(
            dialogueEndDelay
        );

        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(false);
        }
    }


    // ==================================================
    // POLICE SAY
    // ==================================================

    private IEnumerator PoliceSay(string message)
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.color = Color.white;
            dialogueText.gameObject.SetActive(true);
        }

        StartPoliceVoice();

        for (int i = 0; i < message.Length; i++)
        {
            if (dialogueText != null)
            {
                dialogueText.text += message[i];
            }

            yield return new WaitForSeconds(textSpeed);
        }

        StopPoliceVoice();

        yield return new WaitForSeconds(
            dialogueEndDelay
        );

        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(false);
        }
    }


    // ==================================================
    // PLAYER VOICE
    // ==================================================

    private void StartPlayerVoice()
    {
        if (playerVoiceAudioSource == null)
            return;

        if (playerTalking == null)
            return;

        playerVoiceAudioSource.clip = playerTalking;
        playerVoiceAudioSource.loop = true;
        playerVoiceAudioSource.Play();
    }


    private void StopPlayerVoice()
    {
        if (playerVoiceAudioSource != null)
        {
            playerVoiceAudioSource.Stop();
        }
    }

    // ==================================================
    // POLICE VOICE
    // ==================================================

    private void StartPoliceVoice()
    {
        if (policeVoiceAudioSource == null)
            return;

        if (policeTalking == null)
            return;

        policeVoiceAudioSource.clip = policeTalking;
        policeVoiceAudioSource.loop = true;
        policeVoiceAudioSource.Play();
    }


    private void StopPoliceVoice()
    {
        if (policeVoiceAudioSource != null)
        {
            policeVoiceAudioSource.Stop();
        }
    }


    // ==================================================
    // SOUND
    // ==================================================

    private void PlaySound(
        AudioSource source,
        AudioClip clip
    )
    {
        if (source == null)
            return;

        if (clip == null)
            return;

        source.PlayOneShot(clip);
    }


    private float GetSoundLength(AudioClip clip)
    {
        if (clip == null)
            return 0f;

        return clip.length;
    }


    // ==================================================
    // ENDING SCREEN
    // ==================================================

    private void ShowEndingScreen()
    {
        if (endingScreen != null)
        {
            endingScreen.SetActive(true);
        }
    }

    // ==================================================
    // ENDING DIALOGUE POSITION
    // ==================================================

    private void SetEndingDialoguePosition()
        {
            if (dialogueText == null)
                return;

            if (dialogueRect == null)
                dialogueRect = dialogueText.GetComponent<RectTransform>();

            // Text in die Mitte des Parent-Objektes setzen
            dialogueRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogueRect.anchorMax = new Vector2(0.5f, 0.5f);

            dialogueRect.pivot = new Vector2(0.5f, 0.5f);

            dialogueRect.anchoredPosition = Vector2.zero;

            // Verhindert, dass alte Offsets die Position beeinflussen
            dialogueRect.offsetMin = new Vector2(
                -dialogueRect.sizeDelta.x / 2f,
                -dialogueRect.sizeDelta.y / 2f
            );

            dialogueRect.offsetMax = new Vector2(
                dialogueRect.sizeDelta.x / 2f,
                dialogueRect.sizeDelta.y / 2f
            );
        }




    // ==================================================
    // STATUS
    // ==================================================

    public bool HasEnteredHouse()
    {
        return enteredHouse;
    }


    public bool HasEndingTriggered()
    {
        return endingTriggered;
    }
}

