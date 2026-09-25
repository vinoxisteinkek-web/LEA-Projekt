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

        typingCoroutine = StartCoroutine(TypeNormalMessage(message));
    }

    private IEnumerator TypeNormalMessage(string message)
    {
        if (dialogueText == null)
            yield break;

        dialogueText.text = "";
        dialogueText.gameObject.SetActive(true);

        StartPlayerVoice();

        for (int i = 0; i < message.Length; i++)
        {
            dialogueText.text += message[i];

            yield return new WaitForSeconds(textSpeed);
        }

        StopPlayerVoice();

        typingCoroutine = null;
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

        typingCoroutine = StartCoroutine(TypeIntroMessage(message));
    }

    private IEnumerator TypeIntroMessage(string message)
    {
        if (introText == null)
            yield break;

        introText.text = "";
        introText.gameObject.SetActive(true);

        StartPlayerVoice();

        for (int i = 0; i < message.Length; i++)
        {
            introText.text += message[i];

            yield return new WaitForSeconds(textSpeed);
        }

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
            dialogueText.gameObject.SetActive(false);
        }

        if (introText != null)
        {
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