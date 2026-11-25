using UnityEngine;

// =====================================================
// ZAPFSÄULE: E gedrückt halten zum Tanken.
// Nur aktiv, solange der Ablauf im Schritt "Refuel" ist.
// =====================================================
public class FuelPumpInteractable : WorldInteractable
{
    public GasStationPayFlow flow;
    public AudioSource pumpSound;    // optional, z. B. Zapfgeräusch (Loop)

    [Tooltip("Max. Abstand (m, waagerecht) zwischen Säule und Auto-Karosserie, damit der Schlauch reicht")]
    public float maxCarDistance = 5f;

    // Steht das Auto nah genug an DIESER Säule?
    // Gemessen wird zum naechsten Punkt der Auto-Collider, damit es
    // unabhaengig von Groesse/Skalierung des Autos funktioniert.
    public bool IsCarNear(CarController car) => CarDistance(car) <= maxCarDistance;

    // Waagerechter Abstand Saeule -> naechster Punkt der Auto-Karosserie (m)
    public float CarDistance(CarController car)
    {
        if (car == null) return float.MaxValue;
        Vector3 p = transform.position;
        float best = float.MaxValue;
        foreach (var c in car.GetComponents<Collider>())
        {
            if (c == null || !c.enabled || c.isTrigger) continue;
            Vector3 q = c.ClosestPoint(new Vector3(p.x, c.bounds.center.y, p.z));
            q.y = p.y;
            best = Mathf.Min(best, Vector3.Distance(p, q));
        }
        if (best == float.MaxValue)
        {
            Vector3 cp = car.transform.position; cp.y = p.y;
            best = Vector3.Distance(p, cp) - 1.5f;
        }
        return best;
    }

    void Reset()
    {
        promptText = "[E halten] Tanken";
        holdDuration = 5f;
    }

    public override bool CanInteract(GameObject player)
        => flow != null && flow.stage == GasStationPayFlow.Stage.Refuel && IsCarNear(flow.car);

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
