using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// =====================================================
// DIALOG STARTEN
// Auf ein beliebiges Objekt legen und ein Dialog-Asset reinziehen.
//   Manuell       -> per Script / UnityEvent: Play() aufrufen
//   Beim Start    -> startet, sobald die Szene läuft
//   Beim Betreten -> Trigger-Collider; startet, wenn der Spieler
//                    (oder das Auto mit dem Spieler) reinfährt/-läuft
//   Test-Taste    -> zum Ausprobieren (Standard F9)
// =====================================================
public class DialogueTrigger : MonoBehaviour
{
    public enum StartMode
    {
        [InspectorName("Manuell (Play() aufrufen)")] Manual,
        [InspectorName("Beim Start der Szene")] OnStart,
        [InspectorName("Beim Betreten (Trigger)")] OnTriggerEnter,
        [InspectorName("Test-Taste")] DebugKey
    }

    public DialogueData dialogue;
    public StartMode startMode = StartMode.OnTriggerEnter;
    [Tooltip("Wartezeit in Sekunden, bevor der Dialog startet")]
    public float startDelay = 0f;
    public bool onlyOnce = true;
    public string playerTag = "Player";
    public KeyCode debugKey = KeyCode.F9;

    [Header("Events")]
    [Tooltip("Wird für jedes eventName aus dem Dialog aufgerufen (Zeilen + Antworten)")]
    public UnityEvent<string> onDialogueEvent;
    public UnityEvent onDialogueEnd;

    private bool played;
    private bool pending;

    void Start()
    {
        if (startMode == StartMode.OnStart) Play();
    }

    void Update()
    {
        if (startMode == StartMode.DebugKey && Input.GetKeyDown(debugKey)) Play();
    }

    void OnTriggerEnter(Collider other)
    {
        if (startMode != StartMode.OnTriggerEnter) return;
        if (IsPlayer(other.transform) || (other.attachedRigidbody != null && HasPlayerInside(other.attachedRigidbody.transform)))
            Play();
    }

    public void Play()
    {
        if (dialogue == null || pending || (onlyOnce && played)) return;
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        pending = true;
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        // läuft gerade schon ein Dialog? -> warten
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) yield return null;
        pending = false;

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("Kein DialogueManager in der Szene.", this);
            yield break;
        }

        played = true;
        var player = GameObject.FindWithTag(playerTag);
        DialogueManager.Instance.StartDialogue(dialogue, player,
            e => onDialogueEvent?.Invoke(e),
            () => onDialogueEnd?.Invoke());
    }

    public void ResetPlayed() => played = false;

    private bool IsPlayer(Transform t)
    {
        for (; t != null; t = t.parent)
            if (t.CompareTag(playerTag)) return true;
        return false;
    }

    private bool HasPlayerInside(Transform root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
            if (child.CompareTag(playerTag)) return true;
        return false;
    }
}
