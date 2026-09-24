using UnityEngine;
using TMPro;
using System.Collections;

public class StoryManagerStore : MonoBehaviour
{
    public static StoryManagerStore Instance;

    [Header("Radio Music")]
    [SerializeField] private AudioSource radioAudioSource;

    [Header("Player Voice")]
    [SerializeField] private AudioSource playerVoiceAudioSource;
    [SerializeField] private AudioClip playerTalking;

    [Header("Radio Voice")]
    [SerializeField] private AudioSource radioVoiceAudioSource;
    [SerializeField] private AudioClip radioManTalking;

    [Header("Ambient")]
    [SerializeField] private AudioSource ambientHumAudioSource;
    [SerializeField] private AudioSource cicadaAudioSource;

    [SerializeField] private float outsideAmbientVolume = 0.05f;
    [SerializeField] private float shopAmbientVolume = 0.025f;

    [Header("Forest Event")]
    [SerializeField] private AudioSource forestEventAudioSource;

    [Header("Return Home")]
    [SerializeField] private AudioSource returnHomeAudioSource;
    [SerializeField] private GameObject homeTrigger;

    [Header("Dialogue")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float textSpeed = 0.05f;
    [SerializeField] private float dialogueEndDelay = 1.5f;

    [Header("Tasks")]
    [SerializeField] private TaskUI taskUI;

    private bool lightTurnedOn = false;
    private bool radioTurnedOn = false;
    private bool taskListRead = false;
    private bool newsPlayed = false;

    private bool playerInsideShop = false;
    private bool forestEventPlayed = false;

    private bool goHomeTaskActive = false;

    private bool radioTaskActive = false;
    private bool radioCanBeTurnedOff = false;


    private void Awake()
    {
        Instance = this;

        if (homeTrigger != null)
        {
            homeTrigger.SetActive(false);
        }

        if (returnHomeAudioSource != null)
        {
            returnHomeAudioSource.Stop();
        }

        if (forestEventAudioSource != null)
        {
            forestEventAudioSource.Stop();
        }

        if (radioAudioSource != null)
        {
            radioAudioSource.Stop();
        }
    }


    private void Start()
    {
        SetAmbientVolume(outsideAmbientVolume);
    }


    // ==================================================
    // SHOP
    // ==================================================

    public void EnterShop()
    {
        playerInsideShop = true;

        if (!radioTurnedOn)
        {
            SetAmbientVolume(shopAmbientVolume);
        }
    }


    public void ExitShop()
    {
        playerInsideShop = false;

        if (!radioTurnedOn)
        {
            SetAmbientVolume(outsideAmbientVolume);
        }
    }


    private void SetAmbientVolume(float volume)
    {
        if (ambientHumAudioSource != null)
            ambientHumAudioSource.volume = volume;

        if (cicadaAudioSource != null)
            cicadaAudioSource.volume = volume;
    }


    // ==================================================
    // LICHT AN
    // ==================================================

    public void LightTurnedOn()
    {
        if (lightTurnedOn)
            return;

        lightTurnedOn = true;

        if (taskUI != null)
        {
            taskUI.CompleteLightTask();
        }
    }


    // ==================================================
    // LICHT AUS
    // ==================================================

    public bool CanTurnLightOff()
    {
        if (!taskListRead)
            return false;

        if (taskUI == null)
            return false;

        // Hauptaufgaben müssen fertig sein
        if (!taskUI.AreAllTasksComplete())
            return false;

        // Radio muss ausgeschaltet sein
        if (radioTurnedOn)
            return false;

        return true;
    }


    public void LightTurnedOff()
    {
        lightTurnedOn = false;

        if (taskUI != null)
        {
            taskUI.CompleteLightOffTask();
        }

        CheckIfGoHome();
    }


    // ==================================================
    // TASKLIST LESEN
    // ==================================================

    public void ReadTaskList()
    {
        if (!lightTurnedOn)
            return;

        if (taskListRead)
            return;

        taskListRead = true;

        StartCoroutine(TaskListSequence());
    }


    private IEnumerator TaskListSequence()
    {
        if (taskUI != null)
        {
            taskUI.CompleteTaskListTask();
        }

        yield return StartCoroutine(
            PlayerSay(
                "Hi Cass, Sorry i cant be here today. I've got some task for you. Clean the Floor, Remove the Dust and Take out the Trash. I almost forgot the storage lights are broken"
            )
        );

        yield return new WaitForSeconds(0.5f);

        // Noch NICHT die Hauptaufgaben anzeigen!
        // Zuerst muss der Spieler das Radio einschalten.

        if (taskUI != null)
        {
            taskUI.ShowRadioOnTask();
        }
    }


    // ==================================================
    // RADIO AN
    // ==================================================

    public void TurnOnRadio()
    {
        if (radioTurnedOn)
            return;

        radioTurnedOn = true;

        // Ambient ausschalten
        SetAmbientVolume(0f);

        // Radio-Musik starten
        if (radioAudioSource != null)
        {
            radioAudioSource.loop = true;
            radioAudioSource.Play();
        }

        // Radio-On-Aufgabe abschließen
        if (taskUI != null)
        {
            taskUI.CompleteRadioOnTask();

            // Jetzt erst die Hauptaufgaben anzeigen
            taskUI.ShowMainTasks();
        }
    }



    // ==================================================
    // RADIO AUS
    // ==================================================

    public void StopRadio()
    {
        if (radioAudioSource != null)
        {
            radioAudioSource.Stop();
        }

        radioTurnedOn = false;

        // Ambient wieder einschalten
        if (playerInsideShop)
        {
            SetAmbientVolume(shopAmbientVolume);
        }
        else
        {
            SetAmbientVolume(outsideAmbientVolume);
        }

        // Radio-Off-Aufgabe abschließen
        if (taskUI != null)
        {
            if(taskUI.AreAllTasksComplete())
            {
                taskUI.CompleteRadioOffTask();
            }
        }
    }
    public bool RadioIsOn()
    {
        return radioTurnedOn;
    }

    public bool CanTurnRadioOff()
    {
        if (taskUI == null)
            return false;

        if (!taskUI.AreAllTasksComplete())
            return false;

        return true;
    }


    // ==================================================
    // ERSTE PÜTZE
    // ==================================================

    public void FirstPuddleCleaned()
    {
        if (!taskListRead)
            return;

        if (newsPlayed)
            return;

        newsPlayed = true;

        StartCoroutine(NewsSequence());
    }


    private IEnumerator NewsSequence()
    {
        StopRadio();

        yield return new WaitForSeconds(0.5f);


        yield return StartCoroutine(
            RadioSay(
                "<color=white>We are interrupting the music real quick, for some very important information. There is a killer roaming around the area Emschurches.</color>"
            )
        );


        yield return new WaitForSeconds(1f);


        yield return StartCoroutine(
            PlayerSay(
                "Dang, my area? No way, who is it? I hope it's not Michael!"
            )
        );


        yield return new WaitForSeconds(1f);


        yield return StartCoroutine(
            RadioSay(
                "<color=white>Please stay home and lock all doors and windows. If you notice any strange activities report them to the Police.</color>"
            )
        );

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(
            RadioSay(
                "<color=white>We will now continue with the music. Stay safe and have a nice day.</color>"
            )
        ); 
        
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(
            PlayerSay(
                "Dang, that was scary. I better finish my tasks and go home."
            )
        );

        yield return new WaitForSeconds(1f);
      
        TurnOnRadio();
    }


    // ==================================================
    // PLAYER SPRICHT
    // ==================================================

    private IEnumerator PlayerSay(string message)
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
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

        yield return new WaitForSeconds(dialogueEndDelay);

        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(false);
        }
    }


    // ==================================================
    // RADIO SPRICHT
    // ==================================================

    private IEnumerator RadioSay(string message)
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.gameObject.SetActive(true);
        }

        StartRadioVoice();

        for (int i = 0; i < message.Length; i++)
        {
            if (dialogueText != null)
            {
                dialogueText.text += message[i];
            }

            yield return new WaitForSeconds(textSpeed);
        }

        StopRadioVoice();

        yield return new WaitForSeconds(dialogueEndDelay);

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
    // RADIO VOICE
    // ==================================================

    private void StartRadioVoice()
    {
        if (radioVoiceAudioSource == null)
            return;

        if (radioManTalking == null)
            return;

        radioVoiceAudioSource.clip = radioManTalking;
        radioVoiceAudioSource.loop = true;
        radioVoiceAudioSource.Play();
    }


    private void StopRadioVoice()
    {
        if (radioVoiceAudioSource != null)
        {
            radioVoiceAudioSource.Stop();
        }
    }


    // ==================================================
    // 2. MÜLLBEUTEL
    // ==================================================

    public void SecondTrashBagThrownAway()
    {
        if (forestEventPlayed)
            return;

        forestEventPlayed = true;

        if (forestEventAudioSource != null)
        {
            forestEventAudioSource.Play();
        }
    }


    // ==================================================
    // GO HOME
    // ==================================================

    private void CheckIfGoHome()
    {
        if (goHomeTaskActive)
            return;

        if (taskUI == null)
            return;

        if (!taskUI.AreAllTasksComplete())
            return;

        StartGoHomeTask();
    }


    private void StartGoHomeTask()
    {
        goHomeTaskActive = true;

        if (taskUI != null)
        {
            taskUI.ShowGoHomeTask();
        }

        if (homeTrigger != null)
        {
            homeTrigger.SetActive(true);
        }
    }


    // ==================================================
    // HOME TRIGGER
    // ==================================================

    public void ReachedHomeTrigger()
    {
        if (!goHomeTaskActive)
            return;

        goHomeTaskActive = false;

        if (taskUI != null)
        {
            taskUI.CompleteGoHomeTask();
        }

        if (returnHomeAudioSource != null)
        {
            returnHomeAudioSource.Play();
        }

        if (homeTrigger != null)
        {
            homeTrigger.SetActive(false);
        }
    }
}