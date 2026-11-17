using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// DIALOG-ANZEIGE (Untertitel unten in der Mitte)
// Zeigt ein DialogueData-Asset an. Einmal pro Szene vorhanden.
// Aufruf von überall:
//   DialogueManager.Instance.StartDialogue(meinDialog, player, OnEvent, OnEnde);
// oder einfacher mit der Komponente DialogueTrigger.
//
// Steuerung:
//   Interact-Taste (Settings) / Enter = Text sofort ganz zeigen bzw. weiter
//   zusätzlich Klick / Leertaste, wenn der Spieler gesperrt ist
//   (beim Autofahren nicht, weil Leertaste = Handbremse)
//   Tasten 1-9 (oder Maus, wenn gesperrt) = Antwort wählen
// =====================================================
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI lineText;
    public Transform choiceContainer;
    public Button choiceTemplate;           // inaktive Vorlage, wird kopiert
    public GameObject continueHint;

    [Header("Darstellung")]
    [Tooltip("Sekunden pro Buchstabe (0 = sofort ganzer Text)")]
    public float textSpeed = 0.03f;
    public Color playerChoiceColor = new Color(1f, 0.69f, 0f);

    [Header("Stimme")]
    public AudioSource voiceSource;
    [Tooltip("Standard-Murmeln beim Tippen, wenn eine Zeile keine Sprachaufnahme hat")]
    public AudioClip npcTalking;

    [Header("'Nach dem Audio' ohne Audio-Clip")]
    [Tooltip("Lesezeit pro Buchstabe, falls bei 'Automatisch nach dem Audio' noch kein Clip drin ist")]
    public float readTimePerChar = 0.05f;
    public float minReadTime = 1.5f;

    public bool IsOpen { get; private set; }
    public int ClosedFrame { get; private set; } = -1;

    // Für DialogueDucking: 1 = normal laut, kleiner = leiser
    public float DuckTarget => IsOpen && data != null ? data.ambienceVolumeDuringDialogue : 1f;

    private DialogueData data;
    private DialogueNode current;
    private Action<string> onEvent;
    private Action onEnd;
    private GameObject player;
    private bool lockPlayer;
    private readonly List<Behaviour> frozen = new List<Behaviour>();
    private readonly List<Button> spawned = new List<Button>();
    private Coroutine lineRoutine;
    private bool lineComplete;
    private bool skipTyping;
    private int openedFrame;
    private float voiceBaseVolume = 1f;
    private Color speakerBaseColor = Color.white;

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        if (choiceTemplate != null) choiceTemplate.gameObject.SetActive(false);
        if (voiceSource != null) voiceBaseVolume = voiceSource.volume;
        if (speakerText != null) speakerBaseColor = speakerText.color;
    }

    // =================================
    // START / ENDE
    // =================================
    public void StartDialogue(DialogueData dialogue, GameObject playerObject, Action<string> eventCallback = null, Action endCallback = null)
    {
        if (dialogue == null || IsOpen) return;

        DialogueNode start = dialogue.GetStartNode();
        if (start == null)
        {
            Debug.LogWarning("Dialog '" + dialogue.name + "' hat keine Nodes.", dialogue);
            return;
        }

        data = dialogue;
        onEvent = eventCallback;
        onEnd = endCallback;
        player = playerObject;
        lockPlayer = dialogue.lockPlayer;
        IsOpen = true;
        openedFrame = Time.frameCount;

        if (lockPlayer) FreezePlayer(true);
        panel.SetActive(true);
        ShowNode(start);
    }

    public void EndDialogue()
    {
        if (!IsOpen) return;

        StopVoice();
        if (lineRoutine != null) { StopCoroutine(lineRoutine); lineRoutine = null; }
        ClearChoices();
        panel.SetActive(false);
        IsOpen = false;
        ClosedFrame = Time.frameCount;
        if (lockPlayer) FreezePlayer(false);

        var cb = onEnd;
        data = null; current = null; onEvent = null; onEnd = null;
        cb?.Invoke();
    }

    // =================================
    // ZEILEN
    // =================================
    private void ShowNode(DialogueNode node)
    {
        if (node == null) { EndDialogue(); return; }

        current = node;
        ClearChoices();
        StopVoice();
        if (continueHint != null) continueHint.SetActive(false);

        if (lineRoutine != null) StopCoroutine(lineRoutine);
        lineRoutine = StartCoroutine(PlayNode(node));
    }

    private IEnumerator PlayNode(DialogueNode node)
    {
        lineComplete = false;
        skipTyping = false;

        // 1) Pause vor der Zeile
        if (node.delayBefore > 0f)
        {
            speakerText.gameObject.SetActive(false);
            lineText.text = "";
            yield return new WaitForSeconds(node.delayBefore);
            skipTyping = false;
        }

        // 2) Sprecher
        string speakerName = node.speakerData != null ? node.speakerData.displayName : node.speaker;
        speakerText.text = speakerName;
        speakerText.color = node.speakerData != null ? node.speakerData.nameColor : speakerBaseColor;
        speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speakerName));

        // 3) Event
        if (!string.IsNullOrEmpty(node.eventName))
        {
            onEvent?.Invoke(node.eventName);
            if (!IsOpen || current != node) yield break; // Event hat den Dialog beendet/umgeleitet
        }

        // 4) Sound
        bool hasVoice = node.voiceClip != null;
        if (hasVoice) PlayVoice(node.voiceClip, node.voiceVolume);
        else StartTalkingLoop(node.speakerData);

        // 5) Text tippen
        lineText.text = node.text;
        lineText.maxVisibleCharacters = 0;
        lineText.ForceMeshUpdate();
        int total = lineText.textInfo.characterCount;

        if (textSpeed > 0f)
        {
            float t = 0f;
            int shown = 0;
            while (shown < total && !skipTyping)
            {
                t += Time.deltaTime;
                shown = Mathf.Min(total, Mathf.FloorToInt(t / textSpeed));
                lineText.maxVisibleCharacters = shown;
                yield return null;
            }
        }
        lineText.maxVisibleCharacters = int.MaxValue;
        if (!hasVoice) StopVoice();   // Sprachaufnahme läuft weiter, Murmeln nicht
        lineComplete = true;

        // 6) Antworten?
        if (HasChoices(node)) { ShowChoices(); lineRoutine = null; yield break; }

        // 7) Wie geht's weiter?
        switch (node.advance)
        {
            case DialogueAdvance.Key:
                if (continueHint != null) continueHint.SetActive(true);
                lineRoutine = null;
                yield break;

            case DialogueAdvance.AfterAudio:
                if (hasVoice)
                {
                    while (voiceSource != null && voiceSource.isPlaying) yield return null;
                    if (node.pauseAfterAudio > 0f) yield return new WaitForSeconds(node.pauseAfterAudio);
                }
                else
                {
                    yield return new WaitForSeconds(Mathf.Max(minReadTime, total * readTimePerChar));
                }
                break;

            case DialogueAdvance.AfterSeconds:
                yield return new WaitForSeconds(node.seconds);
                break;
        }

        lineRoutine = null;
        Advance();
    }

    // Zur nächsten Zeile (nach Tastendruck oder automatisch)
    private void Advance()
    {
        if (current == null) return;
        if (lineRoutine != null) { StopCoroutine(lineRoutine); lineRoutine = null; }
        StopVoice();

        string next = current.nextNodeId;
        if (IsEnd(next)) { EndDialogue(); return; }
        if (string.IsNullOrEmpty(next)) { ShowNode(data.GetNext(current)); return; }
        ShowNode(data.Find(next));
    }

    private static bool IsEnd(string id) => !string.IsNullOrEmpty(id) && id.Trim().ToUpperInvariant() == "END";

    private static bool HasChoices(DialogueNode n) => n.choices != null && n.choices.Count > 0;

    // =================================
    // ANTWORTEN
    // =================================
    private void ShowChoices()
    {
        if (lockPlayer)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        for (int i = 0; i < current.choices.Count; i++)
        {
            var choice = current.choices[i];
            var b = Instantiate(choiceTemplate, choiceContainer);
            b.gameObject.SetActive(true);
            var label = b.GetComponentInChildren<TextMeshProUGUI>();
            label.text = (i + 1) + ".  " + choice.text;
            int index = i;
            b.onClick.AddListener(() => Choose(index));
            spawned.Add(b);
        }
    }

    private void Choose(int index)
    {
        if (current == null || !HasChoices(current) || index < 0 || index >= current.choices.Count) return;
        var choice = current.choices[index];

        if (lockPlayer)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        StopVoice();
        if (!string.IsNullOrEmpty(choice.eventName))
            onEvent?.Invoke(choice.eventName);

        if (!IsOpen) return; // Event hat den Dialog evtl. beendet

        if (string.IsNullOrEmpty(choice.nextNodeId) || IsEnd(choice.nextNodeId)) { EndDialogue(); return; }
        ShowNode(data.Find(choice.nextNodeId));
    }

    private void ClearChoices()
    {
        foreach (var b in spawned) if (b != null) Destroy(b.gameObject);
        spawned.Clear();
    }

    // =================================
    // EINGABE
    // =================================
    void Update()
    {
        if (!IsOpen || current == null || Time.frameCount == openedFrame || Time.timeScale == 0f) return;

        bool pressed = InputBindings.GetDown(GameAction.Interact) || Input.GetKeyDown(KeyCode.Return);
        if (lockPlayer)
            pressed |= Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);

        if (!lineComplete)
        {
            if (pressed) skipTyping = true;   // Text sofort ganz zeigen
            return;
        }

        if (HasChoices(current))
        {
            for (int i = 0; i < current.choices.Count && i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                { Choose(i); return; }
            return;
        }

        // Auch bei automatischen Zeilen darf man vorspulen
        if (pressed) Advance();
    }

    // =================================
    // SPIELER EINFRIEREN
    // =================================
    private void FreezePlayer(bool freeze)
    {
        if (freeze)
        {
            frozen.Clear();
            if (player == null) return;
            foreach (var b in player.GetComponentsInChildren<Behaviour>())
            {
                if (b is PlayerController || b is CameraController || b is PlayerInteractor)
                {
                    if (b.enabled) { b.enabled = false; frozen.Add(b); }
                }
            }
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
        }
        else
        {
            foreach (var b in frozen) if (b != null) b.enabled = true;
            frozen.Clear();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // =================================
    // SOUND
    // =================================
    private void PlayVoice(AudioClip clip, float volume)
    {
        if (voiceSource == null) return;
        voiceSource.loop = false;
        voiceSource.clip = clip;
        voiceSource.volume = volume;
        voiceSource.Play();
    }

    private void StartTalkingLoop(SpeakerData speaker)
    {
        if (voiceSource == null) return;
        AudioClip clip = npcTalking;
        if (speaker != null)
        {
            if (!speaker.useTalkingSound) return;
            if (speaker.talkingLoop != null) clip = speaker.talkingLoop;
        }
        if (clip == null) return;
        voiceSource.clip = clip;
        voiceSource.loop = true;
        voiceSource.volume = voiceBaseVolume;
        voiceSource.Play();
    }

    private void StopVoice()
    {
        if (voiceSource != null) voiceSource.Stop();
    }
}
