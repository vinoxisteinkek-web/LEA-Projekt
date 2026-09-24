using UnityEngine;
using TMPro;
using System.Collections;

public class StoryMessageScript : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;

    // =========================
    // TEXT
    // =========================

    public float textSpeed = 0.035f;


    // =========================
    // PLAYER VOICE
    // =========================

    public AudioSource playerVoiceAudioSource;
    public AudioClip playerTalking;


    private Coroutine typingCoroutine;


    private void Start()
    {
        dialogueText.gameObject.SetActive(false);
    }


    // =========================================
    // NACHRICHTE ANZEIGEN
    // =========================================

    public void ShowMessage(string message)
    {
        // Falls gerade noch ein anderer Text geschrieben wird
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Alten Sprechsound stoppen
        StopPlayerVoice();

        // Neue Nachricht starten
        typingCoroutine = StartCoroutine(TypeMessage(message));
    }


    // =========================================
    // TEXT BUCHSTABE FÜR BUCHSTABE
    // =========================================

    private IEnumerator TypeMessage(string message)
    {
        dialogueText.text = "";
        dialogueText.gameObject.SetActive(true);

        // Spieler fängt an zu sprechen
        StartPlayerVoice();

        // Text langsam schreiben
        for (int i = 0; i < message.Length; i++)
        {
            dialogueText.text += message[i];

            yield return new WaitForSeconds(textSpeed);
        }

        // Text ist fertig geschrieben
        StopPlayerVoice();

        typingCoroutine = null;
    }


    // =========================================
    // TEXT AUSBLENDEN
    // =========================================

    public void HideMessage()
    {
        // Laufenden Text stoppen
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // Stimme stoppen
        StopPlayerVoice();

        // Text verstecken
        dialogueText.gameObject.SetActive(false);
    }


    // =========================================
    // PLAYER VOICE START
    // =========================================

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


    // =========================================
    // PLAYER VOICE STOP
    // =========================================

    private void StopPlayerVoice()
    {
        if (playerVoiceAudioSource != null)
        {
            playerVoiceAudioSource.Stop();
        }
    }
}