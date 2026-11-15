using UnityEngine;

// =====================================================
// ZAPFSÄULE: E gedrückt halten zum Tanken.
// Nur aktiv, solange der Ablauf im Schritt "Refuel" ist.
// =====================================================
public class FuelPumpInteractable : WorldInteractable
{
    public GasStationPayFlow flow;
    public AudioSource pumpSound;    // optional, z. B. Zapfgeräusch (Loop)

    void Reset()
    {
        promptText = "[E halten] Tanken";
        holdDuration = 5f;
    }

    public override bool CanInteract(GameObject player)
        => flow != null && flow.stage == GasStationPayFlow.Stage.Refuel;

    public override void OnHold(float t)
    {
        flow.SetRefuelProgress(t);
        if (pumpSound != null && !pumpSound.isPlaying) pumpSound.Play();
    }

    public override void OnHoldReleased()
    {
        if (pumpSound != null) pumpSound.Stop();
    }

    public override void Interact(GameObject player)
    {
        if (pumpSound != null) pumpSound.Stop();
        flow.FinishRefuel();
    }
}
