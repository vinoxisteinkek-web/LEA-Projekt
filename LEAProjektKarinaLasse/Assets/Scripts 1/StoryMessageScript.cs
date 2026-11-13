using UnityEngine;
using TMPro;
using System.Collections;

public class StoryMessageScript : MonoBehaviour
{
    [Header("Normaler Dialog")]
    public TextMeshProUGUI dialogueText;

    [Header("Intro Dialog")]
    public TextMeshProUGUI introText;

    [Header("Text Einstellungen")]
    public float textSpeed = 0.035f;

    [Header("Player Voice")]
    public AudioSource playerVoiceAudioSource;
    public AudioClip playerTalking;

    private Coroutine typingCoroutine;

    private void Start()
    {
        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(false);
        }

        if (introText != null)
        {
            introText.gameObject.SetActive(false);
        }
    }

    // =========================
    // NORMALE DIALOGE
    // =========================

    public void ShowMessage(string message)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        StopPlayerVoice();

        typingCoroutine = StartCoroutine(TypeMessage(dialogueText, message));
    }

    // =========================
    // INTRO
    // =========================

    public void ShowIntroMessage(string message)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        StopPlayerVoice();

        typingCoroutine = StartCoroutine(TypeMessage(introText, message));
    }

    // =========================
    // SCHREIBMASCHINEN-EFFEKT
    // Der Text wird EINMAL komplett gesetzt, danach werden nur die
    // sichtbaren Zeichen hochgezaehlt (maxVisibleCharacters).
    // Vorher wurde pro Buchstabe ein neuer String erzeugt und der
    // komplette Text neu aufgebaut -> unnoetiger Muell fuer den GC.
    // =========================

    private IEnumerator TypeMessage(TextMeshProUGUI target, string message)
    {
        if (target == null)
            yield break;

        target.text = message;
        target.maxVisibleCharacters = 0;
        target.gameObject.SetActive(true);

        StartPlayerVoice();

        WaitForSeconds wait = new WaitForSeconds(textSpeed);

        for (int i = 1; i <= message.Length; i++)
        {
            target.maxVisibleCharacters = i;

            yield return wait;
        }

        target.maxVisibleCharacters = int.MaxValue;

        StopPlayerVoice();

        typingCoroutine = null;
    }

    // =========================
    // TEXT AUSBLENDEN
    // =========================

    public void HideMessage()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        StopPlayerVoice();

        if (dialogueText != null)
        {
            dialogueText.maxVisibleCharacters = int.MaxValue;
            dialogueText.gameObject.SetActive(false);
        }

        if (introText != null)
        {
            introText.maxVisibleCharacters = int.MaxValue;
            introText.gameObject.SetActive(false);
        }
    }

    // =========================
    // PLAYER VOICE
    // =========================

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
}
