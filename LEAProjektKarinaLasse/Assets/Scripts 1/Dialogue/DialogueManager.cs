using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// DIALOG-ANZEIGE
// Zeigt ein DialogueData-Asset an. Einmal pro Szene vorhanden.
// Aufruf von überall:
//   DialogueManager.Instance.StartDialogue(meinDialog, player, OnEvent, OnEnde);
//
// Steuerung:
//   Klick / Leertaste / E  = Text sofort ganz zeigen bzw. weiter
//   Maus oder Tasten 1-9   = Antwort wählen
// Während des Dialogs kann sich der Spieler nicht bewegen/umschauen.
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
    public float textSpeed = 0.03f;
    public Color playerChoiceColor = new Color(1f, 0.69f, 0f);

    [Header("Stimme (optional)")]
    public AudioSource voiceSource;
    public AudioClip npcTalking;

    public bool IsOpen { get; private set; }
    public int ClosedFrame { get; private set; } = -1;

    private DialogueData data;
    private DialogueNode current;
    private Action<string> onEvent;
    private Action onEnd;
    private GameObject player;
    private readonly List<Behaviour> frozen = new List<Behaviour>();
    private readonly List<Button> spawned = new List<Button>();
    private Coroutine typing;
    private bool lineComplete;
    private int openedFrame;

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        if (choiceTemplate != null) choiceTemplate.gameObject.SetActive(false);
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
        IsOpen = true;
        openedFrame = Time.frameCount;

        FreezePlayer(true);
        panel.SetActive(true);
        ShowNode(start);
    }

    public void EndDialogue()
    {
        if (!IsOpen) return;

        StopVoice();
        if (typing != null) StopCoroutine(typing);
        ClearChoices();
        panel.SetActive(false);
        IsOpen = false;
        ClosedFrame = Time.frameCount;
        FreezePlayer(false);

        var cb = onEnd;
        data = null; current = null; onEvent = null; onEnd = null;
        cb?.Invoke();
    }

    // =================================
    // NODES
    // =================================
    private void ShowNode(DialogueNode node)
    {
        if (node == null) { EndDialogue(); return; }

        current = node;
        ClearChoices();
        if (continueHint != null) continueHint.SetActive(false);

        speakerText.text = node.speaker;
        speakerText.gameObject.SetActive(!string.IsNullOrEmpty(node.speaker));

        if (!string.IsNullOrEmpty(node.eventName))
            onEvent?.Invoke(node.eventName);

        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeLine(node.text));
    }

    private IEnumerator TypeLine(string text)
    {
        lineComplete = false;
        lineText.text = text;
        lineText.maxVisibleCharacters = 0;
        lineText.ForceMeshUpdate();
        int total = lineText.textInfo.characterCount;

        StartVoice();
        var wait = new WaitForSeconds(textSpeed);
        for (int i = 1; i <= total; i++)
        {
            lineText.maxVisibleCharacters = i;
            yield return wait;
        }
        FinishLine();
    }

    private void FinishLine()
    {
        if (typing != null) { StopCoroutine(typing); typing = null; }
        StopVoice();
        lineText.maxVisibleCharacters = int.MaxValue;
        lineComplete = true;

        if (current.choices != null && current.choices.Count > 0)
            ShowChoices();
        else if (continueHint != null)
            continueHint.SetActive(true);
    }

    private void ShowChoices()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
        if (current == null || current.choices == null || index < 0 || index >= current.choices.Count) return;
        var choice = current.choices[index];

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (!string.IsNullOrEmpty(choice.eventName))
            onEvent?.Invoke(choice.eventName);

        if (!IsOpen) return; // Event hat den Dialog evtl. beendet
        GoTo(choice.nextNodeId);
    }

    private void GoTo(string nextId)
    {
        if (string.IsNullOrEmpty(nextId) || nextId == "END") { EndDialogue(); return; }
        ShowNode(data.Find(nextId));
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
        if (!IsOpen || Time.frameCount == openedFrame || Time.timeScale == 0f) return;

        bool advance = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return);
        bool click = Input.GetMouseButtonDown(0);

        if (!lineComplete)
        {
            if (advance || click) FinishLine();
            return;
        }

        bool hasChoices = current.choices != null && current.choices.Count > 0;
        if (hasChoices)
        {
            for (int i = 0; i < current.choices.Count && i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                { Choose(i); return; }
        }
        else if (advance || click)
        {
            GoTo(current.nextNodeId);
        }
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

    private void StartVoice()
    {
        if (voiceSource == null || npcTalking == null) return;
        voiceSource.clip = npcTalking;
        voiceSource.loop = true;
        voiceSource.Play();
    }

    private void StopVoice()
    {
        if (voiceSource != null) voiceSource.Stop();
    }
}
