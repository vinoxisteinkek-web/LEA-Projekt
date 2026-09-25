using UnityEngine;
using System.Collections;

public class StoryManagerAfterMarket : MonoBehaviour
{
    public static StoryManagerAfterMarket Instance;

    [Header("Sounds")]
    [SerializeField] private AudioSource storyAudioSource;

    [SerializeField] private AudioClip someoneIsInHouseSound;

    [Header("Ending 1 - Phone")]
    [SerializeField] private AudioClip phoneEndingSound;

    [Header("Ending 2 - Bed")]
    [SerializeField] private AudioClip bedEndingSound;

    [Header("Ending 3 - Killer Door")]
    [SerializeField] private AudioClip killerDoorEndingSound;

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

        PlaySound(someoneIsInHouseSound);
    }


    // ==================================================
    // PHONE
    // ==================================================

    public void InteractWithPhone()
    {
        if (endingTriggered)
            return;

        endingTriggered = true;

        Debug.Log("ENDING 1 - PHONE");

        StartCoroutine(PhoneEnding());
    }


    private IEnumerator PhoneEnding()
    {
        PlaySound(phoneEndingSound);

        yield return new WaitForSeconds(
            GetSoundLength(phoneEndingSound)
        );

        Debug.Log("Phone Ending beendet.");

        // Hier später weitere Ending-Logik einfügen.
    }


    // ==================================================
    // BED
    // ==================================================

    public void InteractWithBed()
    {
        if (endingTriggered)
            return;

        endingTriggered = true;

        Debug.Log("ENDING 2 - BED");

        StartCoroutine(BedEnding());
    }


    private IEnumerator BedEnding()
    {
        PlaySound(bedEndingSound);

        yield return new WaitForSeconds(
            GetSoundLength(bedEndingSound)
        );

        Debug.Log("Bed Ending beendet.");

        // Hier später weitere Ending-Logik einfügen.
    }


    // ==================================================
    // KILLER DOOR
    // ==================================================

    public void InteractWithKillerDoor()
    {
        if (endingTriggered)
            return;

        endingTriggered = true;

        Debug.Log("ENDING 3 - KILLER DOOR");

        StartCoroutine(KillerDoorEnding());
    }


    private IEnumerator KillerDoorEnding()
    {
        PlaySound(killerDoorEndingSound);

        yield return new WaitForSeconds(
            GetSoundLength(killerDoorEndingSound)
        );

        Debug.Log("Killer Door Ending beendet.");

        // Hier später weitere Ending-Logik einfügen.
    }


    // ==================================================
    // SOUND
    // ==================================================

    private void PlaySound(AudioClip clip)
    {
        if (storyAudioSource == null)
            return;

        if (clip == null)
            return;

        storyAudioSource.PlayOneShot(clip);
    }


    private float GetSoundLength(AudioClip clip)
    {
        if (clip == null) 
            return 0f;

        return clip.length;
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