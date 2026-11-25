using UnityEngine;

// =====================================================
// [E] EINSTEIGEN am eigenen Auto
// Liegt auf dem Spielerauto (braucht dessen Collider).
// Nur aktiv, wenn 'leave' es erlaubt (z. B. nach dem Bezahlen).
// =====================================================
public class CarEnterInteractable : WorldInteractable
{
    public GasStationLeave leave;

    void Reset() { promptText = "[E] Einsteigen"; }

    public override bool CanInteract(GameObject player) => leave != null && leave.CanEnterCar;

    public override void Interact(GameObject player)
    {
        if (leave != null) leave.EnterCar();
    }
}
