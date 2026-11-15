using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// =====================================================
// TANKSTELLE - TANKEN UND BEZAHLEN
// Läuft nach dem Aussteigen (GasStationIntro.onPlayerExitedCar -> Begin).
//   1. Refuel: an einer Zapfsäule E halten
//   2. Pay:    in den Shop gehen, mit dem Kassierer reden
//              -> der Dialog muss irgendwo das Event "paid" auslösen
//   3. Done:   onPaid wird ausgelöst (Anschluss für den nächsten Teil)
// =====================================================
public class GasStationPayFlow : MonoBehaviour
{
    public enum Stage { Waiting, Refuel, Pay, Done }

    [Header("Referenzen")]
    public GasStationIntro intro;
    public CarController car;

    [Header("Zustand")]
    public Stage stage = Stage.Waiting;

    [Header("Aufgaben-Texte")]
    public string refuelTask = "Das Auto volltanken";
    public string payTask = "Im Shop bezahlen";
    public string doneTask = "";            // leer = Aufgabe ausblenden

    [Header("Spieler-Sätze (leer = nichts sagen)")]
    public string lineAfterRefuel = "Full. Now I have to pay inside.";
    public string lineAfterPaid = "";

    [Header("Event-Name im Dialog, der das Bezahlen bestätigt")]
    public string paidEventName = "paid";

    [Header("Danach")]
    public UnityEvent onRefueled;
    public UnityEvent onPaid;

    private float fuelAtStart;

    public void Begin()
    {
        if (stage != Stage.Waiting) return;
        stage = Stage.Refuel;
        if (car != null) fuelAtStart = car.fuel;
        intro?.ShowTask(refuelTask);
    }

    // ------- Tanken (von FuelPumpInteractable aufgerufen) -------
    public void SetRefuelProgress(float t)
    {
        if (stage != Stage.Refuel || car == null) return;
        car.fuel = Mathf.Lerp(fuelAtStart, 1f, t);
    }

    public void FinishRefuel()
    {
        if (stage != Stage.Refuel) return;
        stage = Stage.Pay;
        if (car != null) car.fuel = 1f;
        intro?.ShowTask(payTask);
        if (!string.IsNullOrEmpty(lineAfterRefuel) && intro != null) StartCoroutine(intro.PlayerSay(lineAfterRefuel));
        onRefueled?.Invoke();
    }

    // ------- Events aus Dialogen (von CashierInteractable weitergereicht) -------
    public void HandleDialogueEvent(string eventName)
    {
        if (eventName == paidEventName && stage == Stage.Pay)
        {
            stage = Stage.Done;
            StartCoroutine(AfterPaid());
        }
    }

    private IEnumerator AfterPaid()
    {
        // warten, bis der Dialog zu ist
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) yield return null;

        if (intro != null)
        {
            if (string.IsNullOrEmpty(doneTask)) intro.HideTask(); else intro.ShowTask(doneTask);
            if (!string.IsNullOrEmpty(lineAfterPaid)) yield return intro.PlayerSay(lineAfterPaid);
        }
        onPaid?.Invoke();
    }
}
