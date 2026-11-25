using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// =====================================================
// KASSIERER: [E] Sprechen
// Kassierer redet weiss, Spieler orange (gleicher Text wie die
// Selbstgespraeche). Nach dem Tanken gibt es ein kleines Gespraech
// mit Antwortauswahl - ohne Box, nur Text unten in der Mitte.
//
// Auswahl: Tasten 1-9 oder mit der Maus anklicken.
// Jedes Thema kann einmal gefragt werden, danach verschwindet es.
// Die Bezahl-Antwort ist immer dabei und beendet das Gespraech.
// =====================================================
public class CashierInteractable : WorldInteractable
{
    [Serializable]
    public class Topic
    {
        [Tooltip("Was der Spieler auswaehlt / sagt")]
        public string question = "";
        [Tooltip("Antwort des Kassierers (mehrere Zeilen = mehrere Saetze nacheinander)")]
        [TextArea(1, 4)] public List<string> answer = new List<string>();
    }

    public GasStationPayFlow flow;
    public GasStationIntro intro;

    [Header("UI")]
    [Tooltip("Textfeld fuer die Antwortmoeglichkeiten (ohne Hintergrund)")]
    public TextMeshProUGUI choiceText;
    public Color choiceColor = new Color(1f, 1f, 1f, 0.85f);
    public Color choiceHoverColor = new Color(1f, 0.69f, 0f);

    [Header("Stimme Kassierer (Gemurmel beim Tippen)")]
    public AudioClip cashierTalking;

    [Header("Vor dem Tanken")]
    public string lineBeforeRefuel = "Fill up first, then come pay.";

    [Header("Bezahlen")]
    public string greeting = "Evening. Pump one?";
    public Topic payTopic = new Topic
    {
        question = "Yes, pump one. Here you go.",
        answer = new List<string> { "That's forty. ...Thanks." }
    };

    [Header("Weitere Antwortmoeglichkeiten")]
    public List<Topic> topics = new List<Topic>
    {
        new Topic { question = "Quiet night, huh?",
                    answer = new List<string> { "Always is, out here.", "Most people don't stop. Not after dark." } },
        new Topic { question = "Did you hear about the girl from Emschurches?",
                    answer = new List<string> { "...", "Everybody heard. Nobody saw anything." } },
    };

    [Header("Nach dem Bezahlen")]
    public string lineAfterPaid = "Drive safe. Don't stop for anyone.";

    [Header("Optional: Kassierer schaut den Spieler an")]
    public Transform lookAtRoot;

    public UnityEvent onPaid;

    private bool talking;
    private readonly List<Behaviour> frozen = new List<Behaviour>();

    void Reset() { promptText = "[E] Sprechen"; }

    void Awake()
    {
        if (choiceText != null) choiceText.gameObject.SetActive(false);
    }

    public override bool CanInteract(GameObject player) => !talking;

    public override void Interact(GameObject player)
    {
        if (talking) return;
        if (lookAtRoot != null)
        {
            Vector3 d = player.transform.position - lookAtRoot.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) lookAtRoot.rotation = Quaternion.LookRotation(d);
        }
        StartCoroutine(Talk(player));
    }

    // =================================
    // GESPRAECH
    // =================================
    private IEnumerator Talk(GameObject player)
    {
        talking = true;
        if (intro == null) intro = FindAnyObjectByType<GasStationIntro>();
        var stage = flow != null ? flow.stage : GasStationPayFlow.Stage.Pay;

        if (stage == GasStationPayFlow.Stage.Pay)
        {
            Freeze(player, true);
            yield return Npc(greeting);

            var open = new List<Topic>(topics);
            while (true)
            {
                var options = new List<Topic>(open) { payTopic };
                int picked = -1;
                yield return Choose(options, i => picked = i);
                Topic t = options[picked];

                yield return Player(t.question);
                foreach (var line in t.answer) yield return Npc(line);

                if (t == payTopic) break;
                open.Remove(t);
            }

            Freeze(player, false);
            flow?.Pay();
            onPaid?.Invoke();
        }
        else if (stage == GasStationPayFlow.Stage.Done)
        {
            yield return Npc(lineAfterPaid);
        }
        else
        {
            yield return Npc(lineBeforeRefuel);
        }

        talking = false;
    }

    private IEnumerator Npc(string line)
    {
        if (intro != null && !string.IsNullOrEmpty(line)) yield return intro.NpcSay(line, cashierTalking);
    }

    private IEnumerator Player(string line)
    {
        if (intro != null && !string.IsNullOrEmpty(line)) yield return intro.PlayerSay(line);
    }

    // =================================
    // AUSWAHL (Tasten 1-9 oder Maus)
    // =================================
    private IEnumerator Choose(List<Topic> options, Action<int> onPicked)
    {
        if (choiceText == null) { onPicked(options.Count - 1); yield break; }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        choiceText.gameObject.SetActive(true);

        int hover = -2, picked = -1;
        while (picked < 0)
        {
            if (Time.timeScale > 0f)
            {
                for (int i = 0; i < options.Count && i < 9; i++)
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) picked = i;

                int h = LineUnderMouse();
                if (h != hover) { hover = h; Render(options, hover); }
                if (picked < 0 && hover >= 0 && Input.GetMouseButtonDown(0)) picked = hover;
            }
            yield return null;
        }

        choiceText.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        onPicked(picked);
    }

    private void Render(List<Topic> options, int hover)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < options.Count; i++)
        {
            Color c = i == hover ? choiceHoverColor : choiceColor;
            if (i > 0) sb.Append('\n');
            sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(c)).Append('>')
              .Append(i + 1).Append(".  ").Append(options[i].question).Append("</color>");
        }
        choiceText.text = sb.ToString();
        choiceText.ForceMeshUpdate();
    }

    private int LineUnderMouse()
    {
        var canvas = choiceText.canvas;
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (!RectTransformUtility.RectangleContainsScreenPoint(choiceText.rectTransform, Input.mousePosition, cam)) return -1;
        return TMP_TextUtilities.FindIntersectingLine(choiceText, Input.mousePosition, cam);
    }

    // =================================
    // SPIELER EINFRIEREN (waehrend des Gespraechs)
    // =================================
    private void Freeze(GameObject player, bool freeze)
    {
        if (freeze)
        {
            frozen.Clear();
            if (player == null) return;
            foreach (var b in player.GetComponentsInChildren<Behaviour>())
            {
                if (b is PlayerController || b is CameraController || b is PlayerInteractor)
                {
                    if (b is PlayerInteractor pi && pi.promptText != null) pi.promptText.gameObject.SetActive(false);
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
}
