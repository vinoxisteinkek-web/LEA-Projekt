using UnityEngine;
using UnityEngine.Events;

// =====================================================
// KASSIERER: [E] Sprechen -> startet einen Dialog.
// Je nach Fortschritt wird ein anderer Dialog benutzt.
// Leeres Feld = in diesem Abschnitt kann man nicht mit ihm reden.
// Events aus dem Dialog (z. B. "paid") gehen an GasStationPayFlow
// und zusätzlich an onDialogueEvent (für eigene Erweiterungen).
// =====================================================
public class CashierInteractable : WorldInteractable
{
    public GasStationPayFlow flow;

    [Header("Dialoge")]
    [Tooltip("Bevor getankt wurde")]
    public DialogueData dialogueBeforeRefuel;
    [Tooltip("Nach dem Tanken - hier muss das Event 'paid' vorkommen")]
    public DialogueData dialoguePay;
    [Tooltip("Nachdem bezahlt wurde")]
    public DialogueData dialogueAfterPaid;

    [Header("Optional: Kassierer schaut den Spieler an")]
    public Transform lookAtRoot;

    public UnityEvent<string> onDialogueEvent;

    void Reset() { promptText = "[E] Sprechen"; }

    private DialogueData CurrentDialogue()
    {
        if (flow == null) return dialoguePay;
        switch (flow.stage)
        {
            case GasStationPayFlow.Stage.Pay: return dialoguePay;
            case GasStationPayFlow.Stage.Done: return dialogueAfterPaid;
            default: return dialogueBeforeRefuel;
        }
    }

    public override bool CanInteract(GameObject player)
        => CurrentDialogue() != null && DialogueManager.Instance != null && !DialogueManager.Instance.IsOpen;

    public override void Interact(GameObject player)
    {
        if (lookAtRoot != null)
        {
            Vector3 d = player.transform.position - lookAtRoot.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) lookAtRoot.rotation = Quaternion.LookRotation(d);
        }

        DialogueManager.Instance.StartDialogue(CurrentDialogue(), player, e =>
        {
            flow?.HandleDialogueEvent(e);
            onDialogueEvent?.Invoke(e);
        });
    }
}
